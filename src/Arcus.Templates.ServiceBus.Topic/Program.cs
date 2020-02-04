using System;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
#if ExcludeSerilog
#else
using Serilog;
using Serilog.Events;
using Serilog.Sinks.ApplicationInsights.Sinks.ApplicationInsights.TelemetryConverters;
#endif

namespace Arcus.Templates.ServiceBus.Topic
{
    public class Program
    {
#if ExcludeSerilog
#else
        #warning Make sure that the appsettings.json is updated with your Azure Application Insights instrumentation key.
        private const string ApplicationInsightsInstrumentationKeyName = "Telemetry_ApplicationInsights_InstrumentationKey";

#endif
        public static int Main(string[] args)
        {
#if ExcludeSerilog
            CreateHostBuilder(args)
                .Build()
                .Run();

            return 0;
#else
            IConfiguration configuration = new ConfigurationBuilder().AddCommandLine(args).Build();
            var instrumentationKey = configuration.GetValue<string>(ApplicationInsightsInstrumentationKeyName);
            
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.ApplicationInsights(instrumentationKey, new TraceTelemetryConverter())
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
#if ExcludeSerilog
#else
                       .UseSerilog()
#endif
                       .ConfigureServices((hostContext, services) =>
                       {
                           //#error Please provide a valid secret provider, for example Azure Key Vault: https: //security.arcus-azure.net/features/secrets/consume-from-key-vault
                           services.AddSingleton<ISecretProvider>(serviceProvider => new CachedSecretProvider(secretProvider: null));

                           services.AddServiceBusTopicMessagePump<EmptyMessagePump>(
                               "Receive-All", secretProvider => secretProvider.GetRawSecretAsync("ARCUS_SERVICEBUS_CONNECTIONSTRING"));
                           services.AddTcpHealthProbes("ARCUS_HEALTH_PORT");
                       });
        }
    }
}
