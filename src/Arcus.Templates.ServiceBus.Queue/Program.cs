using System;
using Arcus.Security.Core.Caching.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
#if (ExcludeSerilog == false)
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
#endif

namespace Arcus.Templates.ServiceBus.Queue
{
    public class Program
    {
#if (ExcludeSerilog == false)
        #warning Make sure that the appsettings.json is updated with your Azure Application Insights instrumentation key.
        private const string ApplicationInsightsInstrumentationKeyName = "TELEMETRY_APPLICATIONINSIGHTS_INSTRUMENTATIONKEY";

#endif
        public static int Main(string[] args)
        {
#if ExcludeSerilog
            CreateHostBuilder(args)
                .Build()
                .Run();

            return 0;
#else
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
#if (ExcludeSerilog == false)
                       .UseSerilog(UpdateLoggerConfiguration)
#endif
                       .ConfigureServices((hostContext, services) =>
                       {
                           services.AddServiceBusQueueMessagePump(secretProvider => secretProvider.GetRawSecretAsync("ARCUS_SERVICEBUS_CONNECTIONSTRING"))
                                   .WithServiceBusMessageHandler<EmptyMessageHandler, EmptyMessage>();
                           
                           services.AddTcpHealthProbes("ARCUS_HEALTH_PORT");
                       });
        }
#if (ExcludeSerilog == false)

        private static void UpdateLoggerConfiguration(
            HostBuilderContext hostContext,
            LoggerConfiguration currentLoggerConfiguration)
        {
            var instrumentationKey = hostContext.Configuration.GetValue<string>(ApplicationInsightsInstrumentationKeyName);

            currentLoggerConfiguration
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithVersion()
                .Enrich.WithComponentName("Service Bus Queue Worker")
                .WriteTo.Console()   
                .WriteTo.AzureApplicationInsights(instrumentationKey);
        }
#endif
    }
}
