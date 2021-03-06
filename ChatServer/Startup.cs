﻿using System;
using System.IO;
using System.Net.WebSockets;
using ChatServer.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatServer
{
    public class Startup
    {
        private static IHostingEnvironment env;

        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            Startup.env = env;
            DefaultAvatar = File.ReadAllBytes(Path.Combine(WwwRootPath, "images/noAvatar.png"));
        }

        public IConfiguration Configuration { get; }

        public static string WwwRootPath => env.WebRootPath;
        public static byte[] DefaultAvatar { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseLoginMiddleware();

            var webSocketOptions = new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(5),
                ReceiveBufferSize = Program.WEBSOCKET_BUFFER_LENGTH
            };
            app.UseWebSockets(webSocketOptions);

            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws" && context.LoginId()>0)
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        var id = context.LoginId();
                        
                        if (!LoginMiddleware.WebSockets.ContainsKey(id))
                        {
                            Console.WriteLine($"User {id} logged in.");
                            await WebSocketManager.BroadcastLogIn(id);
                        }

                        LoginMiddleware.WebSockets[context.LoginId()] = webSocket;
                        await WebSocketManager.Process(context, webSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }

            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    "default",
                    "{controller=Home}/{action=Index}");
            });

            
        }
    }
    
}
