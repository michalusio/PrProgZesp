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

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
