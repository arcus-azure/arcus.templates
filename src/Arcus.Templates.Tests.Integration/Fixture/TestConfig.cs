using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arcus.Templates.Tests.Integration.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using GuardNet;

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
            TargetFramework targetFramework = TargetFramework.NetCoreApp22)
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
        /// Gets the project directory of the Service Bus project based on the given <paramref name="entity"/>.
        /// </summary>
        public DirectoryInfo GetServiceBusProjectDirectory(ServiceBusEntity entity)
        {
            switch (entity)
            {
                case ServiceBusEntity.Queue: return PathCombineWithSourcesDirectory("Arcus.Templates.ServiceBus.Queue");
                case ServiceBusEntity.Topic: return PathCombineWithSourcesDirectory("Arcus.Templates.ServiceBus.Topic");
                default:
                    throw new ArgumentOutOfRangeException(nameof(entity), entity, "Unknown Service Bus entity");
            }
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
        public Uri CreateWebApiBaseUrl()
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
        /// Gets the TCP port on which the worker projects on docker run on.
        /// </summary>
        public int GetDockerWorkerHealthPort()
        {
            const string tcpPortKey = "Arcus:Worker:HealthPort";

            return _configuration.GetValue<int>(tcpPortKey);
        }

        /// <summary>
        /// Gets the Service Bus connection string based on the given <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public string GetServiceBusConnectionString(ServiceBusEntity entity)
        {
            switch (entity)
            {
                case ServiceBusEntity.Queue: return _configuration["Arcus:Worker:ServiceBus:ConnectionStringWithQueue"];
                case ServiceBusEntity.Topic: return _configuration["Arcus:Worker:ServiceBus:ConnectionStringWithTopic"];
                default:
                    throw new ArgumentOutOfRangeException(nameof(entity), entity, "Unknown Service Bus entity");
            }
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
