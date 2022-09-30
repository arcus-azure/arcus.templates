using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Security.Core.Caching.Configuration;
using Arcus.Templates.AzureFunctions.EventHubs;
using Arcus.Templates.AzureFunctions.EventHubs.Model;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
#if Serilog_AppInsights
using Serilog;
using Serilog.Events;
using Serilog.Configuration;
#endif
 
[assembly: FunctionsStartup(typeof(Startup))]
 
namespace Arcus.Templates.AzureFunctions.EventHubs
{
    public class Startup : FunctionsStartup
    {
        // This method gets called by the runtime. Use this method to configure the app configuration.
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            builder.ConfigurationBuilder.AddEnvironmentVariables();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="builder">The instance to build the registered services inside the functions app.</param>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.ConfigureSecretStore((context, config, stores) =>
            { 
//[#if DEBUG]
                stores.AddConfiguration(config);
//[#endif]

                //#error Please provide a valid secret provider, for example Azure Key Vault: https://security.arcus-azure.net/features/secret-store/provider/key-vault
                stores.AddAzureKeyVaultWithManagedIdentity("https://your-keyvault.vault.azure.net/", CacheConfiguration.Default);
            });
            
            builder.Services.AddEventHubsMessageRouting()
                            .WithEventHubsMessageHandler<SensorReadingAzureEventHubsMessageHandler, SensorReading>();
#if Serilog_AppInsights
            
            LoggerConfiguration logConfig = CreateLoggerConfiguration(builder);
            builder.Services.AddLogging(logging =>
            {
                logging.RemoveMicrosoftApplicationInsightsLoggerProvider()
                       .AddSerilog(logConfig.CreateLogger(), dispose: true);
            }); 
#endif
        }
#if Serilog_AppInsights
        
        private static LoggerConfiguration CreateLoggerConfiguration(IFunctionsHostBuilder builder)
        {
            IConfiguration appConfig = builder.GetContext().Configuration;
            var logConfig = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithComponentName("EventHubs Trigger")
                .Enrich.WithVersion()
                .WriteTo.Console();
            
            var connectionString = appConfig.GetValue<string>("APPLICATIONINSIGHTS_CONNECTION_STRING");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                logConfig.WriteTo.AzureApplicationInsightsWithConnectionString(connectionString);
            }
            
            return logConfig;
        } 
#endif
    }
}
