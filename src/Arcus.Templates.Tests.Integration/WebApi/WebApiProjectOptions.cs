﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arcus.Templates.Tests.Integration.Fixture;
using GuardNet;

namespace Arcus.Templates.Tests.Integration.WebApi
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
        public static WebApiProjectOptions Empty { get; } = new WebApiProjectOptions();

        /// <summary>
        /// Adds the project option to include an 'appsettings.json' file to the web API project.
        /// </summary>
        public WebApiProjectOptions WithIncludeAppSettings()
        {
            ProjectOptions optionsWithIncludeAppSettings = AddOption("--include-appsettings");

            return new WebApiProjectOptions(optionsWithIncludeAppSettings);
        }

        /// <summary>
        /// Adds the project option to exclude the correlation capability from the web API project.
        /// </summary>
        public WebApiProjectOptions WithExcludeCorrelation()
        {
            ProjectOptions optionsWithExcludeCorrelation = AddOption("--exclude-correlation");

            return new WebApiProjectOptions(optionsWithExcludeCorrelation);
        }

        /// <summary>
        /// Adds the project option to exclude the correlation capability to the web API project.
        /// </summary>
        public WebApiProjectOptions WithExcludeOpenApiDocs()
        {
            ProjectOptions optionsWithExcludeOpenApi = AddOption("--exclude-openApi");

            return new WebApiProjectOptions(optionsWithExcludeOpenApi);
        }

        /// <summary>
        /// Adds the default logging option to the web API project.
        /// </summary>
        /// <returns></returns>
        public WebApiProjectOptions WithConsoleLogging()
        {
            ProjectOptions optionsWithDefaultLogging = AddOption("--logging Console");

            return new WebApiProjectOptions(optionsWithDefaultLogging);
        }

        /// <summary>
        /// Adds the Serilog logging option to the web API project; writing both to the console and to Azure Application Insights.
        /// </summary>
        /// <param name="applicationInsightsInstrumentationKey">The key to connect to the Azure Application Insights resource.</param>
        public WebApiProjectOptions WithSerilogLogging(string applicationInsightsInstrumentationKey)
        {
            ProjectOptions optionsWithSerilogLogging = 
                AddOption("--logging Serilog", 
                          (fixtureDirectory, projectDirectory) => ConfigureSerilogLogging(fixtureDirectory, projectDirectory, applicationInsightsInstrumentationKey));
            
            return new WebApiProjectOptions(optionsWithSerilogLogging);
        }

        private static void ConfigureSerilogLogging(DirectoryInfo fixtureDirectory, DirectoryInfo projectDirectory, string applicationInsightsInstrumentationKey)
        {
            ReplaceProjectFileContent(
                projectDirectory, 
                "appsettings.json", 
                contents => contents.Replace("<your-application-insights-instrumentation-key>", applicationInsightsInstrumentationKey));

        }

        /// <summary>
        /// Adds a JWT (JSON web token) authentication to the web API project.
        /// </summary>
        /// <param name="key">The security key that was used to generated the JWT token.</param>
        /// <param name="issuer">The issuer that was used to generate the JWT token.</param>
        /// <param name="audience">The audience that was used to generate the JWT token.</param>
        public WebApiProjectOptions WithJwtAuthentication(string key, string issuer, string audience)
        {
            Guard.NotNullOrWhitespace(key, nameof(key), "Cannot add JWT authentication authentication option without a security key to validate the JWT token");

            ProjectOptions optionsWithJwtAuthentication = AddOption(
                "--authentication JWT",
                (fixtureDirectory, projectDirectory) => ConfigureJwtAuthentication(fixtureDirectory, projectDirectory, key, issuer, audience));

            return new WebApiProjectOptions(optionsWithJwtAuthentication);
        }

        private static void ConfigureJwtAuthentication(DirectoryInfo fixtureDirectory, DirectoryInfo projectDirectory, string key, string issuer, string audience)
        {
            AddInMemorySecretProviderFixtureFileToProject(fixtureDirectory, projectDirectory);

            ReplaceProjectFileContent(
                projectDirectory,
                "Startup.cs",
                contents =>
                {
                    contents = InsertInMemorySecretProviderCode(contents, "null", "JwtSigningKey", key);
                    return RemoveCustomUserErrors(contents);
                });

            ReplaceProjectFileContent(
                projectDirectory, 
                "Program.cs", 
                contents =>
                {
                    contents = InsertInMemorySecretStore(contents, "JwtSigningKey", key);
                    return RemoveCustomUserErrors(contents);
                });

            ReplaceProjectFileContent(
                projectDirectory,
                "appsettings.json",
                contents => contents.Replace("<your-issuer>", issuer)
                                    .Replace("<your-audience>", audience));
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
                contents => InsertSharedAccessAuthenticationHeaderSecretPair(contents, requestHeader, secretName));

            ReplaceProjectFileContent(
                projectDirectory,
                "Program.cs",
                contents =>
                {
                    contents = InsertInMemorySecretStore(contents, secretName, secretValue);
                    return RemoveCustomUserErrors(contents);
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

        private static string InsertInMemorySecretStore(string contents, string secretName, string secretValue)
        {
            string newSecretProviderWithSecret = CreatesInMemorySecretProviderConstructor(secretName, secretValue);
            return contents.Replace("AddAzureKeyVaultWithManagedServiceIdentity(\"https://your-keyvault.vault.azure.net/\")", $"AddProvider({newSecretProviderWithSecret})");
        }

        private static string InsertInMemorySecretProviderCode(string contents, string replacementToken, string secretName, string secretValue)
        {
            string newSecretProviderWithSecret = CreatesInMemorySecretProviderConstructor(secretName, secretValue);
            return contents.Replace(replacementToken, newSecretProviderWithSecret);
        }

        private static string CreatesInMemorySecretProviderConstructor(string secretName, string secretValue)
        {
            return $"new {typeof(InMemorySecretProvider).FullName}("
                   + $"new {typeof(Dictionary<string, string>).Namespace}.{nameof(Dictionary<string, string>)}<string, string> {{ [\"{secretName}\"] = \"{secretValue}\" }})";
        }

        private static string InsertSharedAccessAuthenticationHeaderSecretPair(string startupContent, string requestHeader, string secretName)
        {
            return startupContent.Replace("X-API-Key", requestHeader)
                                 .Replace("<your-secret-name>", secretName);
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
                appSettingsContent => appSettingsContent.Replace("<your-certificate-subject-name>", subject));
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
