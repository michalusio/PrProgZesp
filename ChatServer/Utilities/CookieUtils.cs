using System;
using Microsoft.AspNetCore.Http;

namespace ChatServer.Utilities
{
    public static class CookieUtils
    {
        public static string Get(HttpContext context, string variable)
        {
            return context.Request.Cookies.TryGetValue(variable, out var value) ? value : null;
        }

        public static void Set(HttpContext context, string variable, string value)
        {
            if (value == null)
            {
                context.Response.Cookies.Delete(variable);
            }
            else
            {
                context.Response.Cookies.Append(
                    variable,
                    value,
                    new CookieOptions
                    {
                        Path = "/",
                        Expires = new DateTimeOffset(DateTime.Now.AddHours(1)),
                        SameSite = SameSiteMode.Strict
                    }
                );
            }
        }
    }
}