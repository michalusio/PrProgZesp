using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChatServer.Model;
using ChatServer.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ChatServer
{
    public static class WebSocketManager
    {
        public static async Task Process(HttpContext context, WebSocket webSocket)
        {
            while (webSocket.State==WebSocketState.Open && !context.RequestAborted.IsCancellationRequested)
            {
                await CheckMessage(context, webSocket);
            }

            try
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", context.RequestAborted);
            }
            catch (Exception)
            {
                Console.WriteLine("Websocket not closed normally");
            }
            
            webSocket.Dispose();
            var id = context.LoginId();
            SetClearingThread(id);
        }

        private static void SetClearingThread(int id)
        {
            new Thread(async () =>
            {
                Thread.Sleep(2000);
                if (!LoginMiddleware.WebSockets.ContainsKey(id) || LoginMiddleware.WebSockets[id].State != WebSocketState.Open)
                {
                    LoginMiddleware.LogOutUser(id);
                    Console.WriteLine($"User {id} logged out.");
                    var msg = JsonConvert.SerializeObject(new {id, type = "logout"});
                    foreach (var ws in LoginMiddleware.WebSockets)
                    {
                        await ws.Value.SendAsync(Encoding.UTF8.GetBytes(msg), WebSocketMessageType.Text, true,
                            CancellationToken.None);
                    }
                }
            }){IsBackground = true}.Start();
        }

        public static async Task BroadcastLogIn(int id)
        {
            using (var c = new WebChat())
            {
                var user = c.Users.Single(u => u.Id == id);
                var msg = JsonConvert.SerializeObject(
                    new
                    {
                        id = user.Id,
                        type = "login",
                        user = user.Nickname,
                        avatar = Convert.ToBase64String(user.Avatar ?? Startup.DefaultAvatar)
                    });
                foreach (var ws in LoginMiddleware.WebSockets.Where(a =>
                    a.Value.State == WebSocketState.Open))
                {
                    await ws.Value.SendAsync(Encoding.UTF8.GetBytes(msg), WebSocketMessageType.Text,
                        true, CancellationToken.None);
                }
            }
        }

        public static async Task CheckMessage(HttpContext context, WebSocket webSocket)
        {
            try
            {
                var buffer = new byte[Program.WEBSOCKET_BUFFER_LENGTH];
                var message = "";
                WebSocketReceiveResult result;
                do
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    message += Encoding.UTF8.GetString(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                dynamic obj = JsonConvert.DeserializeObject(message);
                switch (obj.type.ToString())
                {
                    case "message":
                        await ReceiveMessage(context, obj);
                        break;
                    default:
                        Console.WriteLine("Unidentified message: "+message);
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Błąd w trakcie odczytu wiadomości: "+e.Message);
            }
        }

        private static async Task ReceiveMessage(HttpContext context,dynamic obj)
        {
            var uid = context.LoginId();
            int id = int.Parse(obj.id.ToString());
            var msg = obj.message.ToString();
            using (var c = new WebChat())
            {
                var conv = c.Conversations
                    .Include(co=>co.ConversationParticipants)
                    .SingleOrDefault(co => co.Id == id);
                if (conv != null && conv.ConversationParticipants.Any(cp => cp.UserId == uid))
                {
                    var msgObject = new Messages
                    {
                        Conversation = conv,
                        Text = MessageUtils.Reformat(msg),
                        UserId = context.LoginId()
                    };
                    c.Add(msgObject);
                    c.SaveChanges();
                    await BroadcastMessage(c, msgObject, uid);
                }
            }
        }

        private static async Task BroadcastMessage(WebChat c, Messages msgObject, int uid)
        {
            var user = c.Users.Single(u => u.Id == uid);
            var msg = JsonConvert.SerializeObject(
                new
                {
                    from = user.Nickname,
                    type = "message",
                    id = msgObject.ConversationId,
                    text = msgObject.Text
                });
            var userIds = msgObject.Conversation.ConversationParticipants.Select(cp => cp.UserId).ToList();
            foreach (var ws in LoginMiddleware.WebSockets.Where(a => userIds.Contains(a.Key) &&
                a.Value.State == WebSocketState.Open)
            )
            {
                msgObject.Conversation.ConversationParticipants.Single(cp => cp.UserId == ws.Key).SeenMessage =
                    msgObject.Id;
                await ws.Value.SendAsync(Encoding.UTF8.GetBytes(msg), WebSocketMessageType.Text,
                    true, CancellationToken.None);
            }
            c.SaveChanges();
        }
    }
}