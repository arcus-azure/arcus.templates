using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Arcus.Template.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args)
                .Build()
                .Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            const string httpEndpointUrl = "http://+:5000";

            return WebHost.CreateDefaultBuilder(args)
                          .UseUrls(httpEndpointUrl)
                          .ConfigureLogging(logging => logging.AddConsole())
                          .UseStartup<Startup>();
        }
    }
}
