using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace ChatServer
{
    public class Program
    {
        public const int WEBSOCKET_BUFFER_LENGTH = 1024 * 4;

        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseUrls("http://localhost:5001/")
                .Build();

    }
}
