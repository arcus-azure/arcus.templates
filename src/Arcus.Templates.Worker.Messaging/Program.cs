using System;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
#if Serilog
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Extensions.Hosting;
#endif

namespace Arcus.Templates.Worker.Messaging
{
    public class Program
    {
#if Serilog
        #warning Make sure that the Azure Application Insights connection string key is available as a secret.
        private const string ApplicationInsightsConnectionStringKeyName = "APPLICATIONINSIGHTS_CONNECTION_STRING";
        
#endif
        public static async Task<int> Main(string[] args)
        {
#if Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateBootstrapLogger();
            
            try
            {
                IHost host = CreateHostBuilder(args).Build();
                await ConfigureSerilogAsync(host);
                await host.RunAsync();
                
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
                       .UseSerilog(Log.Logger)
#endif
                       .ConfigureServices((hostContext, services) =>
                       {
#if ServiceBusQueue
                           services.AddServiceBusQueueMessagePump(secretProvider => secretProvider.GetRawSecretAsync("ARCUS_SERVICEBUS_CONNECTIONSTRING"))
                                   .WithServiceBusMessageHandler<EmptyMessageHandler, EmptyMessage>();
#endif
#if ServiceBusTopic
                               services.AddServiceBusTopicMessagePump("Receive-All", secretProvider => secretProvider.GetRawSecretAsync("ARCUS_SERVICEBUS_CONNECTIONSTRING"))
                                       .WithServiceBusMessageHandler<EmptyMessageHandler, EmptyMessage>();
#endif

                           services.AddTcpHealthProbes("ARCUS_HEALTH_PORT");
                       });
        }
#if Serilog
        
        private static async Task ConfigureSerilogAsync(IHost host)
        {
            var secretProvider = host.Services.GetRequiredService<ISecretProvider>();
            string connectionString = await secretProvider.GetRawSecretAsync(ApplicationInsightsConnectionStringKeyName);
            
            var reloadLogger = (ReloadableLogger) Log.Logger;
            reloadLogger.Reload(config =>
            {
                config.MinimumLevel.Debug()
                      .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                      .Enrich.FromLogContext()
                      .Enrich.WithVersion()
#if ServiceBusQueue
                      .Enrich.WithComponentName("Service Bus Queue Worker") 
#endif
#if ServiceBusTopic
                      .Enrich.WithComponentName("Service Bus Topic Worker")
#endif
                      .WriteTo.Console();
                
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    config.WriteTo.AzureApplicationInsightsWithConnectionString(connectionString);
                }
                
                return config;
            });
        }
#endif
    }
}
