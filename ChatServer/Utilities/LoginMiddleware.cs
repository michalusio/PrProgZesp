using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ChatServer.Utilities
{
    public class LoginMiddleware
    {
        public static readonly Map<string, int> LoggedUsers = new Map<string, int>();
        public static readonly Dictionary<int, Status> Statuses = new Dictionary<int, Status>();

        private readonly RequestDelegate next;

        public LoginMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public static bool LogUser(HttpContext context, int userId)
        {
            if (LoggedUsers.ContainsValue(userId))
            {
                return false;
            }

            var guid = CreateCryptographicallySecureGuid().ToString();
            LoggedUsers.Add(guid, userId);
            Statuses.Add(userId, Status.Available);
            CookieUtils.Set(context, "login", guid);
            return true;
        }

        public static bool LogOutUser(string userKey)
        {
            return LoggedUsers.ContainsKey(userKey) && Statuses.Remove(LoggedUsers.Forward[userKey]) && LoggedUsers.Remove(userKey);
        }

        public static bool LogOutUser(int userId)
        {
            return LoggedUsers.ContainsValue(userId) && LoggedUsers.Remove(userId) && Statuses.Remove(userId);
        }

        public Task InvokeAsync(HttpContext context)
        {
            var credential = CookieUtils.Get(context, "login");
            context.Items["loginId"] = credential == null ? 0 : LoggedUsers.Forward[credential];

            return next(context);
        }

        public static Guid CreateCryptographicallySecureGuid()
        {
            using (var provider = new RNGCryptoServiceProvider()) 
            {
                var bytes = new byte[16];
                provider.GetBytes(bytes);

                return new Guid(bytes);
            }
        }
    }
}