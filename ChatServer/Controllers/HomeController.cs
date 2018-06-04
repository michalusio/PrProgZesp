using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ChatServer.Model;
using ChatServer.Models;
using ChatServer.Utilities;
using Microsoft.AspNetCore.Http;
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
            if (id > 0) LoginMiddleware.LogOutUser(id);
            CookieUtils.Set(HttpContext, "login", null);
            return new JsonResult(new { isLoggedOut = true });
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

        //Metoda dodająca użytkownika jako przyjaciel. Wymaga zalogowania
        [HttpPost]
        public JsonResult AddUserAsFriend(int id)
        {
            if (HttpContext.LoginId() > 0)
            {
                using (var c = new WebChat())
                {
                    var u1 = c.Users.FirstOrDefault(u => u.Id == HttpContext.LoginId());
                    var u2 = c.Users.FirstOrDefault(u => u.Id == id);
                    if (u1 != null && u2 != null)
                    {
                        int count = 0;
                        foreach (Friends f in c.Friends)
                        {
                            var friendOne = f.Friend1 == u1.Id &&
                                            f.Friend2 == u2.Id;
                            var friendTwo = f.Friend1 == u2.Id &&
                                            f.Friend2 == u1.Id;
                            if (friendOne || friendTwo) count++;
                        }

                        if (count == 0)
                        {
                            var friendLink = new Friends
                            {
                                Friend1Navigation = u1,
                                Friend2Navigation = u2
                            };
                            c.Add(friendLink);
                            c.SaveChanges();
                            return new JsonResult(new {added = true});
                        }
                    }

                    return new JsonResult(new {added = false});
                }
            }
            return new JsonResult("");
        }

        [HttpPost]
        public JsonResult BlockUser(int id)
        {
            if (HttpContext.LoginId() > 0)
            {
                using (var c = new WebChat())
                {
                    var u1 = c.Users.FirstOrDefault(u => u.Id == HttpContext.LoginId());
                    var u2 = c.Users.FirstOrDefault(u => u.Id == id);
                    if (u1 != null && u2 != null)
                    {
                        int count = 0;
                        foreach (Blocks f in c.Blocks)
                        {
                            var blockOne = f.Block1 == u1.Id &&
                                            f.Block2 == u2.Id;
                            var blockTwo = f.Block1 == u2.Id &&
                                            f.Block2 == u1.Id;
                            if (blockOne || blockTwo) count++;
                        }

                        if (count == 0)
                        {
                            var blockLink = new Blocks
                            {
                                Block1Navigation = u1,
                                Block2Navigation = u2
                            };
                            c.Add(blockLink);
                            c.SaveChanges();
                            return new JsonResult(new { added = true });
                        }
                    }

                    return new JsonResult(new { added = false });
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

        //Metoda zmieniająca avatar użytkownika. Wymaga zalogowania
        [HttpPost]
        public async void SetAvatar(IFormFile file)
        {
            if (HttpContext.LoginId() > 0)
            {
                if (file == null)
                {
                    return;
                }

                if (file.Length > 2097152)
                {
                    return;
                }

                byte[] avatar;
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    avatar = stream.ToArray();
                }

                using (var c = new WebChat())
                {
                    var user = c.Users.First(u => u.Id == HttpContext.LoginId());
                    user.Avatar = avatar;
                    c.SaveChanges();
                    Console.WriteLine("AVATAR SET");
                }
            }
        }

        //Metoda zmieniająca ustawienia użytkownika. Wymaga zalogowania
        [HttpPost]
        public void SetProperty(string type, string value)
        {
            if (HttpContext.LoginId() > 0)
            {
                if (type == null)
                {
                    return;
                }

                if (value == null)
                {
                    return;
                }

                using (var c = new WebChat())
                {
                    var user = c.Users.First(u => u.Id == HttpContext.LoginId());
                    switch (type)
                    {
                        case "password":
                            user.Password = BCrypt.Net.BCrypt.HashPassword(value);
                            break;
                        case "email":
                            user.Email = value;
                            break;
                        case "nickname":
                            user.Nickname = value;
                            break;
                        case "phone":
                            user.Phone = value;
                            break;
                    }
                    c.SaveChanges();
                    Console.WriteLine($"PROPERTY {type} SET");
                }
            }
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
