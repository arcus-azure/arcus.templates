using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arcus.Security.Secrets.Core.Interfaces;
using Arcus.WebApi.Security.Authentication.Certificates;
using GuardNet;
using Microsoft.AspNetCore.Authorization;

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
        /// Adds Open API security definitions on authorized API endpoints of the web API project.
        /// </summary>
        public WebApiProjectOptions WithOpenApiSecurityDefinitions()
        {
            ProjectOptions optionsWithOpenApiSecurityDefinitions = AddOption(
                "--openapi-security-definitions", 
                (fixtureDirectory, projectDirectory) => { });

            return new WebApiProjectOptions(optionsWithOpenApiSecurityDefinitions);
        }

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
                "--authentication SharedAccessKey",
                (fixtureDirectory, projectDirectory) => ConfigureSharedAccessAuthentication(fixtureDirectory, projectDirectory, headerName, secretName, secretValue));

            return new WebApiProjectOptions(optionsWithSharedAccessAuthentication);
        }

        private static void ConfigureSharedAccessAuthentication(DirectoryInfo fixtureDirectory, DirectoryInfo projectDirectory, string requestHeader, string secretName, string secretValue)
        {
            AddInMemorySecretProviderFixtureFileToProject(fixtureDirectory, projectDirectory);

            ReplaceProjectFileContent(
                projectDirectory,
                "Startup.cs",
                startupContent =>
                {
                    startupContent = InsertInMemorySecretProviderCode(startupContent, secretName, secretValue);
                    startupContent = InsertSharedAccessAuthenticationHeaderSecretPair(startupContent, requestHeader, secretName);

                    return RemoveCustomUserErrors(startupContent);
                });
        }

        private static void AddInMemorySecretProviderFixtureFileToProject(DirectoryInfo fixtureDirectory, DirectoryInfo projectDirectory)
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

        private static string InsertInMemorySecretProviderCode(string startupContent, string secretName, string secretValue)
        {
            string newSecretProviderWithSecret = 
                $"new {typeof(InMemorySecretProvider).FullName}("
                + $"new {typeof(Dictionary<string, string>).Namespace}.{nameof(Dictionary<string, string>)}<string, string> {{ [\"{secretName}\"] = \"{secretValue}\" }})";

            return startupContent.Replace("secretProvider: null", newSecretProviderWithSecret);
        }

        private static string InsertSharedAccessAuthenticationHeaderSecretPair(string startupContent, string requestHeader, string secretName)
        {
            return startupContent.Replace("YOUR REQUEST HEADER NAME", requestHeader)
                                 .Replace("YOUR SECRET NAME", secretName);
        }

        private static string RemoveCustomUserErrors(string content)
        {
            return content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                          .Where(line => !line.Contains("#error"))
                          .Aggregate((line1, line2) => line1 + Environment.NewLine + line2);
        }

        /// <summary>
        /// Adds a certificate authentication on the issuer name to the web API project.
        /// </summary>
        /// <param name="subject">The subject name of the certificate that is allowed by the web API project.</param>
        public WebApiProjectOptions WithCertificateSubjectAuthentication(string subject)
        {
            Guard.NotNullOrWhitespace(subject, nameof(subject), "Cannot add certificate authentication project option based on subject without a subject value");

           ProjectOptions optionsWithCertificateAuthentication = AddOption(
               "--authentication Certificate",
               (fixtureDirectory, projectDirectory) => ConfigureCertificateSubjectAuthentication(projectDirectory, subject));

           return new WebApiProjectOptions(optionsWithCertificateAuthentication);
        }

        private static void ConfigureCertificateSubjectAuthentication(DirectoryInfo projectDirectory, string subject)
        {
            ReplaceProjectFileContent(
                projectDirectory,
                "appsettings.json",
                appSettingsContent => appSettingsContent.Replace("YOUR CERTIFICATE SUBJECT NAME", subject));
        }

        private static void ReplaceProjectFileContent(DirectoryInfo projectDirectory, string fileName, Func<string, string> replacements)
        {
            string filePath = Path.Combine(projectDirectory.FullName, fileName);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Cannot find {filePath} to replace content", filePath);
            }

            string content = File.ReadAllText(filePath);
            content = replacements(content);

            File.WriteAllText(filePath, content);
        }
    }
}
