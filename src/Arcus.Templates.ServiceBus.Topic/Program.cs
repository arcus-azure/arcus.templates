using System;
using Arcus.Security.Core.Caching.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
#if Serilog
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
#endif

namespace Arcus.Templates.ServiceBus.Topic
{
    public class Program
    {
#if Serilog
        #warning Make sure that the appsettings.json is updated with your Azure Application Insights instrumentation key.
        private const string ApplicationInsightsInstrumentationKeyName = "TELEMETRY_APPLICATIONINSIGHTS_INSTRUMENTATIONKEY";

#endif
        public static int Main(string[] args)
        {
#if Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
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
            return Host.CreateDefaultBuilder(args)
                       .ConfigureAppConfiguration(configuration =>
                       {
                           configuration.AddCommandLine(args);
                           configuration.AddEnvironmentVariables();
                       })
                       .ConfigureSecretStore((config, stores) =>
                       {
//[#if DEBUG]
                           stores.AddConfiguration(config);
//[#endif]

                           //#error Please provide a valid secret provider, for example Azure Key Vault: https://security.arcus-azure.net/features/secret-store/provider/key-vault
                           stores.AddAzureKeyVaultWithManagedIdentity("https://your-keyvault.vault.azure.net/", CacheConfiguration.Default);
                       })
#if Serilog
                       .UseSerilog(UpdateLoggerConfiguration)
#endif
                       .ConfigureServices((hostContext, services) =>
                       {
                           services.AddServiceBusTopicMessagePump("Receive-All", secretProvider => secretProvider.GetRawSecretAsync("ARCUS_SERVICEBUS_CONNECTIONSTRING"))
                                   .WithServiceBusMessageHandler<EmptyMessageHandler, EmptyMessage>();
                           
                           services.AddTcpHealthProbes("ARCUS_HEALTH_PORT");
                       });
        }
#if Serilog

        private static void UpdateLoggerConfiguration(
            HostBuilderContext hostContext,
            LoggerConfiguration config)
        {
            config.MinimumLevel.Debug()
                  .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                  .Enrich.FromLogContext()
                  .Enrich.WithVersion()
                  .Enrich.WithComponentName("Service Bus Topic Worker")
                  .WriteTo.Console();

            var instrumentationKey = hostContext.Configuration.GetValue<string>(ApplicationInsightsInstrumentationKeyName);
            if (instrumentationKey != null)
            {
                config.WriteTo.AzureApplicationInsights(instrumentationKey);
            }
        }
#endif
    }
}
