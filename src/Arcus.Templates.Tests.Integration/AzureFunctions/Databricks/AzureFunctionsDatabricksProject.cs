using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.AzureFunctions.Databricks.Configuration;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Worker.Fixture;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Databricks
{
    public class AzureFunctionsDatabricksProject : TemplateProject
    {
        private const string DatabricksUrlVariable = "Arcus__Databricks__Url",
                             ApplicationInsightsInstrumentationKeyVariable = "Arcus__ApplicationInsights__InstrumentationKey",
                             ApplicationInsightsMetricNameVariable = "Arcus__ApplicationInsights__MetricName";

        private readonly AzureFunctionsConfig _azureFunctionsConfig;

        private AzureFunctionsDatabricksProject(
            TestConfig configuration, 
            ITestOutputHelper outputWriter) 
            : base(configuration.GetAzureFunctionsDatabricksProjectDirectory(), 
                   configuration.GetFixtureProjectDirectory(), 
                   outputWriter)
        {
            _azureFunctionsConfig = configuration.GetAzureFunctionsConfig();

            DatabricksConfig = configuration.GetDatabricksConfig();
            ApplicationInsightsConfig = configuration.GetApplicationInsightsConfig();
        }

        /// <summary>
        /// Gets the Databricks connectivity information from the current application configuration, used by this project.
        /// </summary>
        public DatabricksConfig DatabricksConfig { get; }

        /// <summary>
        /// Gets the Application Insights connectivity information from the current application configuration, used  by this project.
        /// </summary>
        public ApplicationInsightsConfig ApplicationInsightsConfig { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="outputWriter"></param>
        /// <returns></returns>
        public static AzureFunctionsDatabricksProject StartNew(TestConfig configuration, ITestOutputHelper outputWriter)
        {
            AzureFunctionsDatabricksProject project = CreateNew(configuration, outputWriter);
            project.Start();

            return project;
        }

        private static AzureFunctionsDatabricksProject CreateNew(TestConfig configuration, ITestOutputHelper outputWriter)
        {
            var project = new AzureFunctionsDatabricksProject(configuration, outputWriter);
            project.CreateNewProject(new ProjectOptions());
            project.AddDatabricksSecurityToken(project.DatabricksConfig.SecurityToken);
            project.AddStorageAccount(project._azureFunctionsConfig.StorageAccountConnectionString);
            
            return project;
        }

        private void Start()
        {
            Run(BuildConfiguration.Debug, TargetFramework.NetCoreApp31);
        }

        private void AddDatabricksSecurityToken(string securityToken)
        {
            AddTypeAsFile<SingleValueSecretProvider>();

            UpdateFileInProject("Startup.cs", contents =>
                RemovesUserErrorsFromContents(contents)
                    .Replace("AddAzureKeyVaultWithManagedServiceIdentity(\"https://your-keyvault-vault.azure.net/\")", 
                             $"AddProvider(new {nameof(SingleValueSecretProvider)}(\"{securityToken}\"))"));
        }

        private void AddStorageAccount(string storageAccountConnectionString)
        {
            UpdateFileInProject("local.settings.json", 
                contents => contents.Replace("UseDevelopmentStorage=true", storageAccountConnectionString));
        }

        protected override ProcessStartInfo PrepareProjectRun(
            BuildConfiguration buildConfiguration,
            TargetFramework targetFramework,
            CommandArgument[] commandArguments)
        {
            var processInfo = new ProcessStartInfo("func", "start")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = ProjectDirectory.FullName,
            };

            Environment.SetEnvironmentVariable(DatabricksUrlVariable, DatabricksConfig.BaseUrl);
            Environment.SetEnvironmentVariable(ApplicationInsightsInstrumentationKeyVariable, ApplicationInsightsConfig.InstrumentationKey);
            Environment.SetEnvironmentVariable(ApplicationInsightsMetricNameVariable, ApplicationInsightsConfig.MetricName);

            return processInfo;
        }

        protected override void Disposing(bool disposing)
        {
            Environment.SetEnvironmentVariable(DatabricksUrlVariable, null);
            Environment.SetEnvironmentVariable(ApplicationInsightsInstrumentationKeyVariable, null);
            Environment.SetEnvironmentVariable(ApplicationInsightsMetricNameVariable, null);
        }
    }
}
