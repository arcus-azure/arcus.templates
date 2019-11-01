using System.Collections.Generic;
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
                    .AddInMemoryCollection(new []{new KeyValuePair<string, string>("CertificateSubject", "YOUR KEY TO CERTIFICATE SUBJECT NAME") })
                    .AddEnvironmentVariables()
                    .Build();

            string httpEndpointUrl = "http://+:" + configuration["ARCUS_HTTP_PORT"];
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