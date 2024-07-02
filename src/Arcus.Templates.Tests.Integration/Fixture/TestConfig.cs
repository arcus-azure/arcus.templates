using System;
using System.Collections.Generic;
using System.IO;
using Arcus.Templates.Tests.Integration.AzureFunctions.Configuration;
using Arcus.Templates.Tests.Integration.AzureFunctions.Http.Configuration;
using Arcus.Templates.Tests.Integration.Worker.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using GuardNet;
using Microsoft.Extensions.Logging;
using Arcus.Templates.Tests.Integration.Worker.EventHubs.Fixture;

namespace Arcus.Templates.Tests.Integration.Fixture
{
    /// <summary>
    /// Configuration implementation with test values used in test cases to simulate scenario's.
    /// </summary>
    public class TestConfig : IConfigurationRoot
    {
        private readonly IConfigurationRoot _configuration;

        private TestConfig(
            IConfigurationRoot configuration, 
            BuildConfiguration buildConfiguration,
            TargetFramework targetFramework)
        {
            Guard.NotNull(configuration, nameof(configuration));

            _configuration = configuration;

            BuildConfiguration = buildConfiguration;
            TargetFramework = targetFramework;
        }

        /// <summary>
        /// Gets the build configuration for the project created from the template.
        /// </summary>
        public BuildConfiguration BuildConfiguration { get; }

        /// <summary>
        /// Gets the target framework for the project created from the template.
        /// </summary>
        public TargetFramework TargetFramework { get; }

        /// <summary>
        /// Creates a new <see cref="IConfigurationRoot"/> with test values.
        /// </summary>
        /// <param name="buildConfiguration">The configuration in which the created project from the template should be build.</param>
        /// <param name="targetFramework">The target framework in which the created project from the template should be build and run.</param>
        public static TestConfig Create(
            BuildConfiguration buildConfiguration = BuildConfiguration.Debug,
            TargetFramework targetFramework = TargetFramework.Net8_0)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(path: "appsettings.json", optional: true)
                .AddJsonFile(path: "appsettings.local.json", optional: true)
                .AddJsonFile(path: "appsettings.private.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            return new TestConfig(configuration, buildConfiguration, targetFramework);
        }

        /// <summary>
        /// Gets the project directory of the web API project.
        /// </summary>
        public DirectoryInfo GetWebApiProjectDirectory()
        {
            return PathCombineWithSourcesDirectory("Arcus.Templates.WebApi");
        }

        /// <summary>
        /// Gets the project directory of the Service Bus project based on the given <paramref name="entityType"/>.
        /// </summary>
        public DirectoryInfo GetServiceBusProjectDirectory(ServiceBusEntityType entityType)
        {
            switch (entityType)
            {
                case ServiceBusEntityType.Queue: return PathCombineWithSourcesDirectory("Arcus.Templates.ServiceBus.Queue");
                case ServiceBusEntityType.Topic: return PathCombineWithSourcesDirectory("Arcus.Templates.ServiceBus.Topic");
                default:
                    throw new ArgumentOutOfRangeException(nameof(entityType), entityType, "Unknown Service Bus entity");
            }
        }

        public DirectoryInfo GetEventHubsProjectDirectory()
        {
            return PathCombineWithSourcesDirectory("Arcus.Templates.EventHubs");
        }

        /// <summary>
        /// Gets the project directory of the Azure Functions Service Bus project based on the given <paramref name="entityType"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when no project directory can be found for the given <paramref name="entityType"/>.</exception>
        public DirectoryInfo GetAzureFunctionsServiceBusProjectDirectory(ServiceBusEntityType entityType)
        {
            switch (entityType)
            {
                case ServiceBusEntityType.Queue: return PathCombineWithSourcesDirectory("Arcus.Templates.AzureFunctions.ServiceBus.Queue");
                case ServiceBusEntityType.Topic: return PathCombineWithSourcesDirectory("Arcus.Templates.AzureFunctions.ServiceBus.Topic");
                default:
                    throw new ArgumentOutOfRangeException(nameof(entityType), entityType, "Unknown Service Bus entity type");
            }
        }

        /// <summary>
        /// Gets the project directory of the Azure Functions EventHubs project template.
        /// </summary>
        public DirectoryInfo GetAzureFunctionsEventHubsProjectDirectory()
        {
            return PathCombineWithSourcesDirectory("Arcus.Templates.AzureFunctions.EventHubs");
        }

        /// <summary>
        /// Gets the project directory of the Azure Functions Databricks project template.
        /// </summary>
        public DirectoryInfo GetAzureFunctionsHttpProjectDirectory()
        {
            return PathCombineWithSourcesDirectory("Arcus.Templates.AzureFunctions.Http");
        }

        /// <summary>
        /// Gets the project directory of the integration test project template.
        /// </summary>
        public DirectoryInfo GetIntegrationTestTemplateProjectDirectory()
        {
            return PathCombineWithSourcesDirectory("Arcus.Templates.IntegrationTests");
        }

        /// <summary>
        /// Gets the project directory where the fixtures are located.
        /// </summary>
        public DirectoryInfo GetFixtureProjectDirectory()
        {
            return PathCombineWithSourcesDirectory(typeof(TestConfig).Assembly.GetName().Name);
        }

        private DirectoryInfo PathCombineWithSourcesDirectory(string subPath)
        {
            DirectoryInfo sourcesDirectory = GetBuildSourcesDirectory();

            string path = Path.Combine(sourcesDirectory.FullName, "src", subPath);
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException(
                    $"Cannot find sub-directory in build sources directory at: {path}");
            }

            return new DirectoryInfo(path);
        }

        private DirectoryInfo GetBuildSourcesDirectory()
        {
            const string buildSourcesDirectory = "Build.SourcesDirectory";

            string sourcesDirectory = _configuration.GetValue<string>(buildSourcesDirectory);
            Guard.NotNull(sourcesDirectory, nameof(sourcesDirectory), $"No build sources directory configured with the key: {buildSourcesDirectory}");
            Guard.For<ArgumentException>(
                () => !Directory.Exists(sourcesDirectory),
                $"No directory exists at {Path.GetFullPath(sourcesDirectory)}");

            return new DirectoryInfo(sourcesDirectory);
        }

        /// <summary>
        /// Gets the base URL of the to-be-created project from the web API template.
        /// </summary>
        /// <returns></returns>
        public Uri GetDockerBaseUrl()
        {
            Uri baseUrl = GetBaseUrl();
            return baseUrl;
        }

        private static readonly Random RandomPort = new Random();

        /// <summary>
        /// Gets the base URL of the to-be-created project from the web API template.
        /// </summary>
        public Uri GenerateRandomLocalhostUrl()
        {
            Uri baseUrl = GetBaseUrl();

            int port = RandomPort.Next(8080, 9000);
            return new Uri($"http://localhost:{port}{baseUrl.AbsolutePath}");
        }
        
        private Uri GetBaseUrl()
        {
            const string baseUrlKey = "Arcus:Api:BaseUrl";

            var baseUrl = _configuration.GetValue<string>(baseUrlKey);
            Guard.NotNull(baseUrl, nameof(baseUrl), $"No base URL configured with the key: {baseUrlKey}");

            if (!Uri.TryCreate(baseUrl, UriKind.RelativeOrAbsolute, out Uri result))
            {
                throw new InvalidOperationException(
                    $"Cannot create valid URI from configured base URL with the key: {baseUrlKey}");
            }

            return result;
        }

        /// <summary>
        /// Generates a new TCP port for self-containing worker projects.
        /// </summary>
        public int GenerateWorkerHealthPort()
        {
            return RandomPort.Next(8080, 9000);
        }

        /// <summary>
        /// Gets the TCP port on which the Service Bus Queue worker projects on docker run on.
        /// </summary>
        public int GetDockerServiceBusQueueWorkerHealthPort()
        {
            const string tcpPortKey = "Arcus:Worker:ServiceBus:Queue:HealthPort";

            return _configuration.GetValue<int>(tcpPortKey);
        }

        /// <summary>
        /// Gets the TCP port on which the Service Bus topic worker projects on docker run on.
        /// </summary>
        public int GetDockerServiceBusTopicWorkerHealthPort()
        {
            const string tcpPortKey = "Arcus:Worker:ServiceBus:Topic:HealthPort";

            return _configuration.GetValue<int>(tcpPortKey);
        }

        /// <summary>
        /// Gets the TCP port on which the Service Bus topic worker projects on docker run on.
        /// </summary>
        public int GetDockerEventHubsWorkerHealthPort()
        {
            const string tcpPortKey = "Arcus:Worker:EventHubs:HealthPort";

            return _configuration.GetValue<int>(tcpPortKey);
        }

        /// <summary>
        /// Gets the Service Bus connection string based on the given <paramref name="entity"/>.
        /// </summary>
        public string GetServiceBusConnectionString(ServiceBusEntityType entityType)
        {
            switch (entityType)
            {
                case ServiceBusEntityType.Queue: return _configuration["Arcus:Worker:ServiceBus:Queue:ConnectionString"];
                case ServiceBusEntityType.Topic: return _configuration["Arcus:Worker:ServiceBus:Topic:ConnectionString"];
                default:
                    throw new ArgumentOutOfRangeException(nameof(entityType), entityType, "Unknown Service Bus entity");
            }
        }

        /// <summary>
        /// Gets all the configuration to run the Azure EventHubs integration tests.
        /// </summary>
        public EventHubsConfig GetEventHubsConfig()
        {
            return new EventHubsConfig(
                _configuration.GetValue<string>("Arcus:Worker:EventHubs:EventHubsName"),
                _configuration.GetValue<string>("Arcus:Worker:EventHubs:ConnectionString"),
                _configuration.GetValue<string>("Arcus:Worker:EventHubs:BlobStorage:StorageAccountConnectionString"));
        }

        /// <summary>
        /// Gets the instrumentation key to access the Azure Application Insights resource.
        /// </summary>
        public string GetApplicationInsightsInstrumentationKey()
        {
            const string key = "Arcus:Api:ApplicationInsights:InstrumentationKey";

            return _configuration.GetValue<string>(key);
        }

        /// <summary>
        /// Gets the configuration model to use an Azure Event Grid resource.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when one or more configuration values cannot be found.</exception>
        public EventGridConfig GetEventGridConfig()
        {
            var eventGridTopicUri = _configuration.GetRequiredValue<string>("Arcus:Worker:EventGrid:TopicUri");
            var eventGridAuthKey = _configuration.GetRequiredValue<string>("Arcus:Worker:EventGrid:AuthKey");

            return new EventGridConfig(eventGridTopicUri, eventGridAuthKey);
        }

        /// <summary>
        /// Gets the Azure Functions application configuration to create valid Azure Functions projects.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when one of the Azure Functions configuration values are not found.</exception>
        public AzureFunctionsConfig GetAzureFunctionsConfig()
        {
            var storageAccountConnectionString = _configuration.GetRequiredValue<string>("Arcus:AzureFunctions:AzureWebJobsStorage");

            return new AzureFunctionsConfig(storageAccountConnectionString);
        }

        /// <summary>
        /// Gets the application configuration to interact with the HTTP Azure Function.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when one of the HTTP Azure Function configuration values are not found.</exception>
        public AzureFunctionHttpConfig GetAzureFunctionHttpConfig()
        {
            return new AzureFunctionHttpConfig(
                _configuration.GetRequiredValue<int>("Arcus:AzureFunctions:Http:Isolated:HttpPort"),
                _configuration.GetRequiredValue<int>("Arcus:AzureFunctions:Http:InProcess:HttpPort"));
        }

        /// <summary>
        /// Gets the Azure Application Insights configuration to interact with the Application Insights resource.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when one of the Azure Application Insights configuration values are not found.</exception>
        public ApplicationInsightsConfig GetApplicationInsightsConfig()
        {
            var instrumentationKey = _configuration.GetRequiredValue<string>("Arcus:AzureFunctions:ApplicationInsights:InstrumentationKey");
            var applicationId = _configuration.GetRequiredValue<string>("Arcus:AzureFunctions:ApplicationInsights:ApplicationId");
            var apiKey = _configuration.GetRequiredValue<string>("Arcus:AzureFunctions:ApplicationInsights:ApiKey");
            var metricName = _configuration.GetRequiredValue<string>("Arcus:AzureFunctions:ApplicationInsights:MetricName");

            return new ApplicationInsightsConfig(instrumentationKey, applicationId, apiKey, metricName);
        }

        /// <summary>
        /// Gets or sets a configuration value.
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>The configuration value.</returns>
        public string this[string key] 
        { 
            get => _configuration[key]; 
            set => _configuration[key] = value; 
        }

        /// <summary>
        /// The<see cref="IConfigurationProvider" /> for this configuration.
        /// </summary>
        public IEnumerable<IConfigurationProvider> Providers => _configuration.Providers;

        /// <summary>
        /// Gets the immediate descendant configuration sub-sections.
        /// </summary>
        /// <returns>The configuration sub-sections.</returns>
        public IEnumerable<IConfigurationSection> GetChildren()
        {
            return _configuration.GetChildren();
        }

        /// <summary>
        /// Returns a <see cref="IChangeToken"/> that can be used to observe when this configuration is reloaded.
        /// </summary>
        public IChangeToken GetReloadToken()
        {
            return _configuration.GetReloadToken();
        }

        /// <summary>
        /// Gets a configuration sub-section with the specified key.
        /// </summary>
        /// <param name="key">The key of the configuration section.</param>
        /// <remarks>
        ///     This method will never return null. If no matching sub-section is found with
        ///      the specified key, an empty <see cref="IConfigurationSection" /> will be returned.
        /// </remarks>
        public IConfigurationSection GetSection(string key)
        {
            return _configuration.GetSection(key);
        }

        /// <summary>
        /// Force the configuration values to be reloaded from the underlying <see cref="IConfigurationProvider" />
        /// </summary>
        public void Reload()
        {
            _configuration.Reload();
        }
    }
}
