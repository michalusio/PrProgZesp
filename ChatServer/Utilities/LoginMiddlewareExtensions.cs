using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace ChatServer.Utilities
{
    public static class LoginMiddlewareExtensions
    {
        public static IApplicationBuilder UseLoginMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LoginMiddleware>();
        }

        /// <summary>
        /// Pobiera ID zalogowanego użytkownika. 0, jeśli nie zalogowany
        /// </summary>
        public static int LoginId(this HttpContext ht)
        {
            return (ht.Items["loginId"] as int?).GetValueOrDefault();
        }
    }
}