﻿using System;
using Arcus.Security.Core.Caching.Configuration;
using Arcus.Templates.AzureFunctions.Http;
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

namespace Arcus.Templates.AzureFunctions.Http
{
    public class Startup : FunctionsStartup
    {
        // This method gets called by the runtime. Use this method to configure the app configuration.
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
#if OpenApi
//[#if DEBUG]
            Environment.SetEnvironmentVariable("OpenApi__HideSwaggerUI", "false");
//[#else]
            Environment.SetEnvironmentVariable("OpenApi__HideSwaggerUI", "true");
//[#endif]
#endif
            builder.ConfigurationBuilder.AddEnvironmentVariables();
        }
        
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.AddHttpCorrelation();
#if IncludeHealthChecks
            builder.Services.AddHealthChecks();
#endif
            
            builder.ConfigureSecretStore((context, config, stores) =>
            {
//[#if DEBUG]
                stores.AddConfiguration(config);
//[#endif]

                stores.AddEnvironmentVariables();

                //#error Please provide a valid secret provider, for example Azure Key Vault: https://security.arcus-azure.net/features/secret-store/provider/key-vault
                stores.AddAzureKeyVaultWithManagedIdentity("https://your-keyvault.vault.azure.net/", CacheConfiguration.Default);
            });
#if Serilog_AppInsights

            builder.Services.AddAppName("Azure HTTP trigger");
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
            var appConfig = provider.GetRequiredService<IConfiguration>();
            var connectionString = appConfig.GetValue<string>("APPLICATIONINSIGHTS_CONNECTION_STRING");

            var configuration = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithComponentName(provider)
                .Enrich.WithVersion(provider)
                .WriteTo.Console();
            
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                configuration.WriteTo.AzureApplicationInsightsWithConnectionString(provider, connectionString);
            }
            
            return configuration;
        } 
#endif
    }
}
