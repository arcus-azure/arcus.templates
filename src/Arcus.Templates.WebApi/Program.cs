using System;
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
                    .AddEnvironmentVariables()
                    .Build();

            string httpEndpointUrl = "http://+:" + configuration["ARCUS_HTTP_PORT"];
            IWebHostBuilder webHostBuilder =
                WebHost.CreateDefaultBuilder(args)
                       .UseConfiguration(configuration)
                       .UseUrls(httpEndpointUrl)
#if DefaultLog
                       .ConfigureLogging(logging => logging.AddConsole())
                       .UseStartup<Startup>();
#elif Serilog
                        .UseStartup<Startup>()
                        .UseSerilog();
#endif

            return webHostBuilder;
        }
    }
}
