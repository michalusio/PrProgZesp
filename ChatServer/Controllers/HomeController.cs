using System;
using System.Diagnostics;
using System.Linq;
using ChatServer.Model;
using ChatServer.Models;
using ChatServer.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace ChatServer.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        //Metoda rejestracji. Przyjmuje potrzebne dane, zwraca wartość isLogged określającą, czy udało się zarejestrować i zalogować
        [HttpPost]
        public JsonResult TryRegister(string login, string password, string name, string email, string phone)
        {
            if (HttpContext.LoginId() == 0)
            {
                using (var c = new WebChat())
                {
                    var toLogin = c
                        .Users
                        .Where(u => u.Login.Equals(login));
                    if (toLogin.Count() != 0)
                        return new JsonResult(new {isLogged = false});
                    var user = new Users
                    {
                        CreationDate = DateTime.Now,
                        Login = login,
                        Password = BCrypt.Net.BCrypt.HashPassword(password),
                        Nickname = name,
                        Email = email,
                        Phone = phone
                    };
                    c.Users.Add(user);
                    c.SaveChanges();
                }

                using (var c = new WebChat())
                {
                    var user = c.Users.First(u => u.Login.Equals(login));
                    return new JsonResult(new
                    {
                        isLogged = LoginMiddleware.LogUser(HttpContext, user.Id)
                    });
                }
            }
            return new JsonResult(new {isLogged = false});
        }

        //Metoda logowania. Przyjmuje login i hasło, zwraca wartość isLogged określającą, czy udało się zalogować
        [HttpPost]
        public JsonResult TryLogin(string name, string password)
        {
            using (var c = new WebChat())
            {
                var toLogin = c
                    .Users
                    .Where(u => u.Login.Equals(name) &&
                                BCrypt.Net.BCrypt.Verify(password, u.Password)
                    );
                if (toLogin.Count() != 1)
                    return new JsonResult(new {isLogged = false});
                return new JsonResult(new
                {
                    isLogged = HttpContext.LoginId() == 0 &&
                               LoginMiddleware.LogUser(HttpContext, toLogin.Single().Id)
                });
            }
        }

        //Metoda zmiany statusu. Wymaga bycia zalogowanym
        [HttpPost]
        public JsonResult SetStatus(Status status)
        {
            var id = HttpContext.LoginId();
            if (id > 0)
            {
                LoginMiddleware.Statuses[id] = status;
                return new JsonResult(new { newStatus = (int)status });
            }

            return new JsonResult(new { newStatus = -1 });
        }

        //Metoda wylogowująca użytkownika. Zwraca wartość isLoggedOut określającą, czy udało się wylogować
        [HttpPost]
        public JsonResult TryLogOut()
        {
            var id = HttpContext.LoginId();
            var js = new JsonResult(
                new
                {
                    isLoggedOut = id > 0 && LoginMiddleware.LogOutUser(id)
                }
            );
            CookieUtils.Set(HttpContext, "login", null);
            return js;
        }

        //Metoda wysyłająca użytkownikowi ID konwersacji z drugą osobą. Konwersacja zostanie utworzona w przypadku jej braku. Wymaga zalogowania
        [HttpPost]
        public JsonResult GetUserConversation(int id)
        {
            if (HttpContext.LoginId() > 0)
            {
                using (var c = new WebChat())
                {
                    var conv = c.GetConversationBetween(
                        c.Users
                            .Where(u =>
                                u.Id == id ||
                                u.Id == HttpContext.LoginId()
                            )
                    );
                    if (conv == null)
                    {
                        conv = new Conversations();

                        var p1 = new ConversationParticipants
                        {
                            UserId = HttpContext.LoginId()
                        };
                        conv.ConversationParticipants.Add(p1);

                        var p2 = new ConversationParticipants
                        {
                            UserId = id
                        };
                        conv.ConversationParticipants.Add(p2);

                        c.Add(conv);
                        c.SaveChanges();

                        conv = c.GetConversationBetween(
                            c.Users
                                .Where(u =>
                                    u.Id == id ||
                                    u.Id == HttpContext.LoginId()
                                )
                        );
                    }

                    return new JsonResult(new {id = conv.Id});
                }
            }
            return new JsonResult("");
        }

        //Metoda pobierająca nicki użytkowników konwersacji. Wymaga zalogowania
        [HttpPost]
        public JsonResult GetConversationUsers(int id)
        {
            if (HttpContext.LoginId() > 0)
            {
                using (var c = new WebChat())
                {
                    var conv = c.ConversationParticipants.Where(cp => cp.ConversationId == id);
                    var nicknames = conv.Select(cp => cp.User.Nickname);
                    return new JsonResult(new
                    {
                        nicknames = nicknames.ToArray(),
                        seen = conv.First(cp => cp.UserId == HttpContext.LoginId()).SeenMessage
                    });
                }
            }
            return new JsonResult("");
        }
        
        //Metoda pobierająca wiadomości konwersacji. Wymaga zalogowania
        [HttpPost]
        public JsonResult GetConversationMessages(int id)
        {
            if (HttpContext.LoginId() > 0)
            {
                using (var c = new WebChat())
                {
                    var messages = c.Messages
                        .Where(m => m.ConversationId == id)
                        .OrderBy(m => m.Timestamp);
                    var js = new JsonResult(new
                    {
                        messages = messages.Select(m =>
                            new
                            {
                                id = m.Id,
                                from = m.User.Nickname,
                                text = m.Text
                            }).ToArray()
                    });
                    var conv = c.ConversationParticipants.SingleOrDefault(cp =>
                        cp.ConversationId == id &&
                        cp.UserId == HttpContext.LoginId());
                    if (conv != null)
                    {
                        conv.SeenMessage = messages.LastOrDefault()?.Id ?? 0;
                        c.SaveChanges();
                    }
                    return js;
                }
            }
            return new JsonResult("");
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
