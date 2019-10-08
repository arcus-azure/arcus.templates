using System.IO;
using Arcus.Security.Secrets.Core.Interfaces;
using GuardNet;

namespace Arcus.Template.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents all the available project options on the web API template project.
    /// </summary>
    public class WebApiProjectOptions : ProjectOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiProjectOptions"/> class.
        /// </summary>
        public WebApiProjectOptions()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiProjectOptions"/> class.
        /// </summary>
        private WebApiProjectOptions(ProjectOptions options) : base(options)
        {
        }

        /// <summary>
        /// Creates a set of empty project options for the web API project; resulting in a default web API project when a project is created from these options.
        /// </summary>
        public static readonly WebApiProjectOptions Empty = new WebApiProjectOptions();

        /// <summary>
        /// Adds a shared access key authentication to the web API project.
        /// </summary>
        /// <param name="headerName">The name of the request header which value must match the stored secret.</param>
        /// <param name="secretName">The name of the secret that's being retrieved using the <see cref="ISecretProvider.Get"/> call.</param>
        public WebApiProjectOptions WithSharedAccessAuthentication(string headerName, string secretName)
        {
            Guard.NotNull(headerName, nameof(headerName), "Cannot add shared access key authentication project option without a HTTP request header name containing the secret name");
            Guard.NotNull(secretName, nameof(secretName), "Cannot add shared access key authentication project option without a secret name");

            ProjectOptions optionsWithSharedAccessAuthentication = AddOption(
                "--Authentication SharedAccessKey",
                projectDirectory => ConfigureSharedAccessAuthentication(projectDirectory, headerName, secretName));

            return new WebApiProjectOptions(optionsWithSharedAccessAuthentication);
        }

        private void ConfigureSharedAccessAuthentication(DirectoryInfo projectDirectory, string requestHeader, string secretName)
        {
            string srcInMemorySecretProviderFilePath = Path.Combine("Fixture", nameof(InMemorySecretProvider) + ".cs");
            if (!File.Exists(srcInMemorySecretProviderFilePath))
            {
                throw new FileNotFoundException(
                    $"Cannot find {nameof(InMemorySecretProvider)}.cs for stubbing the secret during the authentication",
                    srcInMemorySecretProviderFilePath);
            }

            string destInMemorySecretProviderFilePath = Path.Combine(projectDirectory.FullName, nameof(InMemorySecretProvider) + ".cs");
            File.Copy(srcInMemorySecretProviderFilePath, destInMemorySecretProviderFilePath);

            string startupFilePath = Path.Combine(projectDirectory.FullName, "Startup.cs");
            if (!File.Exists(startupFilePath))
            {
                throw new FileNotFoundException(
                    $"Cannot find Startup.cs to replace the secret provider with a {nameof(InMemorySecretProvider)}",
                    startupFilePath);
            }

            string startupContent = File.ReadAllText(startupFilePath);
            startupContent = 
                startupContent.Replace("secretProvider: null", $"new {typeof(InMemorySecretProvider).FullName}()")
                              .Replace("YOUR REQUEST HEADER NAME", requestHeader)
                              .Replace("YOUR SECRET NAME", secretName);

            File.WriteAllText(startupFilePath, startupContent);
        }
    }
}
