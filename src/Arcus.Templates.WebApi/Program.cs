using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Arcus.Security.Core.Caching.Configuration;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
#if Serilog
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Arcus.Observability.Telemetry.Serilog.Sinks.ApplicationInsights;
#endif

namespace Arcus.Templates.WebApi
{
    public class Program
    {
#if Serilog
        #warning Make sure that the appsettings.json is updated with your Azure Application Insights instrumentation key.
        private const string ApplicationInsightsInstrumentationKeyName = "Telemetry:ApplicationInsights:InstrumentationKey";

#endif
        
        public static int Main(string[] args)
        {
#if Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                CreateHostBuilder(args)
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
            CreateHostBuilder(args)
                .Build()
                .Run();

            return 0;
#endif
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            IConfiguration configuration = CreateConfiguration(args);
            IHostBuilder webHostBuilder = CreateHostBuilder(args, configuration);

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

        private static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration)
        {
            string httpEndpointUrl = "http://+:" + configuration["ARCUS_HTTP_PORT"];
            IHostBuilder webHostBuilder =
                Host.CreateDefaultBuilder(args)
                    .ConfigureAppConfiguration(configBuilder => configBuilder.AddConfiguration(configuration))
                    .ConfigureSecretStore((config, stores) =>
                    {
//[#if DEBUG]
                        stores.AddConfiguration(config);
//[#endif]

                        //#error Please provide a valid secret provider, for example Azure Key Vault: https://security.arcus-azure.net/features/secret-store/provider/key-vault
                        stores.AddAzureKeyVaultWithManagedIdentity("https://your-keyvault.vault.azure.net/", CacheConfiguration.Default);
                    })
#if Serilog
                    .UseSerilog((context, serviceProvider, config) => CreateLoggerConfiguration(context, serviceProvider, config)) 
#endif
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.ConfigureKestrel(kestrelServerOptions => kestrelServerOptions.AddServerHeader = false)
                                  .UseUrls(httpEndpointUrl)
#if Console
                                  .ConfigureLogging(logging => logging.AddConsole())
#endif
                                  .UseStartup<Startup>();
                    });

            return webHostBuilder;
        }
#if Serilog

        private static LoggerConfiguration CreateLoggerConfiguration(
            HostBuilderContext context, 
            IServiceProvider serviceProvider, 
            LoggerConfiguration config)
        {
            var instrumentationKey = context.Configuration.GetValue<string>(ApplicationInsightsInstrumentationKeyName);
            
            return config
                .ReadFrom.Configuration(context.Configuration)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithVersion()
                .Enrich.WithComponentName("API")
#if (ExcludeCorrelation == false)
                .Enrich.WithHttpCorrelationInfo(serviceProvider)
#endif
                .WriteTo.Console()
                .WriteTo.AzureApplicationInsights(instrumentationKey);
        }
#endif
    }
}
