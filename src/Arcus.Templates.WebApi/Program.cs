using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Arcus.Templates.WebApi
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
            IConfigurationRoot configuration =
                new ConfigurationBuilder()
                    .AddCommandLine(args)
                    .AddEnvironmentVariables()
                    .Build();

            string httpEndpointUrl = "http://+:" + 8080;
            IWebHostBuilder webHostBuilder =
                WebHost.CreateDefaultBuilder(args)
                       .ConfigureKestrel(kestrelServerOptions => kestrelServerOptions.AddServerHeader = false)
                       .UseConfiguration(configuration)
                       .UseUrls(httpEndpointUrl)
                       .ConfigureLogging(logging => logging.AddConsole())
                       .UseStartup<Startup>();

            return webHostBuilder;
        }
    }
}