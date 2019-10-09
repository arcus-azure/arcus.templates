using System;
using System.Collections.Generic;
using System.IO;
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

        private TestConfig(IConfigurationRoot configuration, string buildConfiguration)
        {
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNullOrWhitespace(buildConfiguration, nameof(buildConfiguration));

            _configuration = configuration;
            BuildConfiguration = buildConfiguration;
        }

        /// <summary>
        /// Gets the build configuration for the project created from the template.
        /// </summary>
        public string BuildConfiguration { get; }

        /// <summary>
        /// Creates a new <see cref="IConfigurationRoot"/> with test values.
        /// </summary>
        /// <param name="buildConfiguration">The configuration in which the created project from the template should be build.</param>
        public static TestConfig Create(string buildConfiguration = "Debug")
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(path: "appsettings.json", optional: true)
                .AddJsonFile(path: "appsettings.local.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            return new TestConfig(configuration, buildConfiguration);
        }

        /// <summary>
        /// Gets the project file path of the web API project.
        /// </summary>
        public DirectoryInfo GetWebApiProjectDirectory()
        {
            const string webApiProjectPath = "Arcus:Api:ProjectPath";

            string projectPath = _configuration.GetValue<string>(webApiProjectPath);
            Guard.NotNull(projectPath, nameof(projectPath), $"No project path configured for the web API project with the key: {webApiProjectPath}");
            Guard.For<ArgumentException>(
                () => !Directory.Exists(projectPath),
                $"No project template directory exists at {projectPath}");

            return new DirectoryInfo(projectPath);
        }

        /// <summary>
        /// Gets the base URL of the to-be-created project from the web API template.
        /// </summary>
        /// <returns></returns>
        public Uri GetDockerBaseUrl()
        {
            const string baseUrlKey = "Arcus:Api:BaseUrl";

            string baseUrl = _configuration.GetValue<string>(baseUrlKey);
            Guard.NotNull(baseUrl, nameof(baseUrl), $"No base URL configured with the key: {baseUrlKey}");
            
            if (!Uri.TryCreate(baseUrl, UriKind.RelativeOrAbsolute, out Uri result))
            {
                throw new InvalidOperationException($"Cannot create valid URI from configured base URL with the key: {baseUrlKey}");
            }

            return result;
        }

        private static readonly Random RandomPort = new Random();

        /// <summary>
        /// Gets the base URL of the to-be-created project from the web API template.
        /// </summary>
        public Uri CreateWebAPiBaseUrl()
        {
            const string baseUrlKey = "Arcus:Api:BaseUrl";

            string baseUrl = _configuration.GetValue<string>(baseUrlKey);
            Guard.NotNull(baseUrl, nameof(baseUrl), $"No base URL configured with the key: {baseUrlKey}");
            
            if (!Uri.TryCreate(baseUrl, UriKind.RelativeOrAbsolute, out Uri result))
            {
                throw new InvalidOperationException($"Cannot create valid URI from configured base URL with the key: {baseUrlKey}");
            }

            int port = RandomPort.Next(8080, 9000);
            return new Uri($"http://localhost:{port}{result.AbsolutePath}");
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
