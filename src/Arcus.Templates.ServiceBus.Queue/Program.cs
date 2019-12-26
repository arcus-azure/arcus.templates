using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Arcus.Templates.ServiceBus.Queue
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args)
                .Build()
                .Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(configuration =>
                {
                    configuration.AddCommandLine(args);
                    configuration.AddEnvironmentVariables();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    #error Please provide a valid secret provider, for example Azure Key Vault: https: //security.arcus-azure.net/features/secrets/consume-from-key-vault
                    services.AddSingleton<ISecretProvider>(serviceProvider => new CachedSecretProvider(secretProvider: null));

                    services.AddServiceBusQueueMessagePump<EmptyMessagePump>(secretProvider => secretProvider.GetRawSecretAsync("ARCUS_SERVICEBUS_CONNECTIONSTRING"));
                    services.AddTcpHealthProbes();
                });
    }
}
