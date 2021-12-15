using System;
using System.Collections.Generic;
using Arcus.Security.Core.Caching.Configuration;
using Arcus.Templates.AzureFunctions.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Arcus.Templates.AzureFunctions.Http
{
    public class Startup : FunctionsStartup
    {
        // This method gets called by the runtime. Use this method to configure the app configuration.
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
//[#if DEBUG]
            Environment.SetEnvironmentVariable("OpenApi__HideSwaggerUI", "false");
//[#else]
            Environment.SetEnvironmentVariable("OpenApi__HideSwaggerUI", "true");
//[#endif]

            builder.ConfigurationBuilder.AddEnvironmentVariables();
        }
        
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection
        public override void Configure(IFunctionsHostBuilder builder)
        {
            IConfiguration config = builder.GetContext().Configuration;

            builder.AddHttpCorrelation();
#if IncludeHealthChecks
            builder.Services.AddHealthChecks();
#endif

            builder.ConfigureSecretStore(stores =>
            {
//[#if DEBUG]
                stores.AddConfiguration(config);
//[#endif]

                stores.AddEnvironmentVariables();

                //#error Please provide a valid secret provider, for example Azure Key Vault: https://security.arcus-azure.net/features/secret-store/provider/key-vault
                stores.AddAzureKeyVaultWithManagedIdentity("https://your-keyvault.vault.azure.net/", CacheConfiguration.Default);
            });

            builder.Services.AddLogging(loggingBuilder => ConfigureLogging(loggingBuilder, config));
        }

        private static void ConfigureLogging(ILoggingBuilder builder, IConfiguration config)
        {
            var logConfiguration = new LoggerConfiguration()
                                   .ReadFrom.Configuration(config)
                                   .Enrich.FromLogContext()
                                   .Enrich.WithComponentName("Azure HTTP Trigger")
                                   .Enrich.WithVersion()
                                   .WriteTo.Console();

            var telemetryKey = config.GetValue<string>("APPINSIGHTS_INSTRUMENTATIONKEY");

            if (!String.IsNullOrWhiteSpace(telemetryKey))
            {
                logConfiguration.WriteTo.AzureApplicationInsights(telemetryKey);
            }

            builder.ClearProvidersExceptFunctionProviders();
            builder.AddSerilog(logConfiguration.CreateLogger());
        }
    }
}
