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
using Serilog.Sinks.ApplicationInsights.Sinks.ApplicationInsights.TelemetryConverters;    
#endif

namespace Arcus.Templates.WebApi
{
    public class Program
    {
#if Serilog
        private const string ApplicationInsightsInstrumentationKeyName = "ApplicationInsightsInstrumentationKeyName";

#endif
        public static int Main(string[] args)
        {
#if Serilog
            IConfiguration configuration = CreateConfiguration(args);
            var instrumentationKey = configuration.GetValue<string>(ApplicationInsightsInstrumentationKeyName);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.ApplicationInsights(instrumentationKey, new EventTelemetryConverter())
                .CreateLogger();

            try
            {
                CreateWebHostBuilder(args, configuration)
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
            IConfiguration configuration = CreateConfiguration(args);
            IWebHostBuilder webHostBuilder = CreateWebHostBuilder(args, configuration);

            return webHostBuilder;
        }

        private static IConfiguration CreateConfiguration(string[] args)
        {
            IConfigurationRoot configuration =
                new ConfigurationBuilder()
                    .AddCommandLine(args)
#if AppSettings
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
#endif
                    .AddEnvironmentVariables()
                    .Build();

            return configuration;
        }

        private static IWebHostBuilder CreateWebHostBuilder(string[] args, IConfiguration configuration)
        {
            string httpEndpointUrl = "http://+:" + configuration["ARCUS_HTTP_PORT"];
            IWebHostBuilder webHostBuilder =
                WebHost.CreateDefaultBuilder(args)
                       .ConfigureKestrel(kestrelServerOptions => kestrelServerOptions.AddServerHeader = false)
                       .UseConfiguration(configuration)
                       .UseUrls(httpEndpointUrl)
#if Console
                       .ConfigureLogging(logging => logging.AddConsole())
#elif Serilog
                       .UseSerilog()
#endif
                       .UseStartup<Startup>();

            return webHostBuilder;
        }
    }
}