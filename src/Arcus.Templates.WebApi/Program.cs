using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
#if Serilog
using Serilog;
using Serilog.Events;
#endif

namespace Arcus.Templates.WebApi
{
    public class Program
    {
        public static int Main(string[] args)
        {
#if Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                CreateWebHostBuilder(args)
                    .Build()
                    .Run();

                return 0;
            }
            catch (Exception exception)
            {
                Log.Fatal(exception, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
#else
            CreateWebHostBuilder(args)
                .Build()
                .Run();

            return 0;
#endif
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
#if DefaultLog
                       .ConfigureLogging(logging => logging.AddConsole())
#elif Serilog
                       .UseSerilog()
#endif
                       .UseStartup<Startup>();

            return webHostBuilder;
        }
    }
}