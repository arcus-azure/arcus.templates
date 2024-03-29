﻿using System;
using Arcus.Security.Core.Caching.Configuration;
using Arcus.Templates.AzureFunctions.ServiceBus.Topic;
using Arcus.Templates.AzureFunctions.ServiceBus.Topic.Model;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
#if Serilog_AppInsights
using Serilog;
using Serilog.Configuration;
using Serilog.Events; 
#endif
 
[assembly: FunctionsStartup(typeof(Startup))]
 
namespace Arcus.Templates.AzureFunctions.ServiceBus.Topic
{
    public class Startup : FunctionsStartup
    {
        // This method gets called by the runtime. Use this method to configure the app configuration.
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            builder.ConfigurationBuilder.AddEnvironmentVariables();
        }
        
        // This method gets called by the runtime. Use this method to add services to the container.
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
            
            builder.AddServiceBusMessageRouting()
                   .WithServiceBusMessageHandler<OrdersAzureServiceBusMessageHandler, Order>();
#if Serilog_AppInsights
            
            builder.Services.AddAppName("Service Bus Topic Trigger");
            builder.Services.AddAssemblyAppVersion<Startup>();
            builder.Services.AddLogging(logging =>
            {
                logging.RemoveMicrosoftApplicationInsightsLoggerProvider()
                       .AddSerilog(provider => CreateLoggerConfiguration(provider).CreateLogger());
            }); 
#endif
        }
#if Serilog_AppInsights
        
        private static LoggerConfiguration CreateLoggerConfiguration(IServiceProvider provider)
        {
            IConfiguration appConfig = provider.GetRequiredService<IConfiguration>();
            var logConfig = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithComponentName(provider)
                .Enrich.WithVersion(provider)
                .WriteTo.Console();
            
            var connectionString = appConfig.GetValue<string>("APPLICATIONINSIGHTS_CONNECTION_STRING");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                logConfig.WriteTo.AzureApplicationInsightsWithConnectionString(provider, connectionString);
            }
            
            return logConfig;
        } 
#endif
    }
}