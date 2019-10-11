using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arcus.Security.Secrets.Core.Interfaces;
using GuardNet;

namespace Arcus.Templates.Tests.Integration.Fixture
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
        /// <param name="secretValue">The value of the secret that should be retrieved using the <see cref="ISecretProvider.Get"/> call.</param>
        public WebApiProjectOptions WithSharedAccessAuthentication(string headerName, string secretName, string secretValue)
        {
            Guard.NotNullOrWhitespace(headerName, nameof(headerName), "Cannot add shared access key authentication project option without a HTTP request header name containing the secret name");
            Guard.NotNullOrWhitespace(secretName, nameof(secretName), "Cannot add shared access key authentication project option without a secret name");
            Guard.NotNullOrWhitespace(secretValue, nameof(secretValue), "Cannot add shared access key authentication project option without a secret value");

            ProjectOptions optionsWithSharedAccessAuthentication = AddOption(
                "--Authentication SharedAccessKey",
                (fixtureDirectory, projectDirectory) => ConfigureSharedAccessAuthentication(fixtureDirectory, projectDirectory, headerName, secretName, secretValue));

            return new WebApiProjectOptions(optionsWithSharedAccessAuthentication);
        }

        private void ConfigureSharedAccessAuthentication(DirectoryInfo fixtureDirectory, DirectoryInfo projectDirectory, string requestHeader, string secretName, string secretValue)
        {
            string srcInMemorySecretProviderFilePath = FindFixtureTypeInDirectory(fixtureDirectory, typeof(InMemorySecretProvider));
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
            string newSecretProviderWithSecret = 
                $"new {typeof(InMemorySecretProvider).FullName}("
                + $"new {typeof(Dictionary<string, string>).Namespace}.{nameof(Dictionary<string, string>)}<string, string> {{ [\"{secretName}\"] = \"{secretValue}\" }})";

            startupContent = 
                startupContent.Replace("secretProvider: null", newSecretProviderWithSecret)
                              .Replace("YOUR REQUEST HEADER NAME", requestHeader)
                              .Replace("YOUR SECRET NAME", secretName);

            File.WriteAllText(startupFilePath, startupContent);
        }

        private static string FindFixtureTypeInDirectory(DirectoryInfo fixtureDirectory, Type fixtureType)
        {
            string fixtureFileName = fixtureType.Name + ".cs";
            IEnumerable<FileInfo> files = 
                fixtureDirectory.EnumerateFiles(fixtureFileName, SearchOption.AllDirectories);

            if (!files.Any())
            {
                throw new FileNotFoundException(
                    $"Cannot find fixture with file name: {fixtureFileName} in directory: {fixtureDirectory.FullName}", 
                    fixtureFileName);
            }

            if (files.Count() > 1)
            {
                throw new IOException(
                    $"More than a single fixture matches the file name: {fixtureFileName} in directory: {fixtureDirectory.FullName}");
            }

            return files.First().FullName;
        }
    }
}
