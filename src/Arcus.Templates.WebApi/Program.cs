using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
#if AppSettings
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
#endif
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