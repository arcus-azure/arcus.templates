using System;
using System.Diagnostics;
using Arcus.Templates.Tests.Integration.AzureFunctions.Databricks.JobMetrics.Configuration;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Worker.Fixture;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Databricks.JobMetrics
{
    /// <summary>
    /// Project template to create new Azure Functions Databricks Job Metrics projects.
    /// </summary>
    [DebuggerDisplay("Project = {ProjectDirectory.FullName}, Databricks URL = {DatabricksUrlVariable}")]
    public class AzureFunctionsDatabricksProject : AzureFunctionsProject
    {
        private const string ApplicationInsightsMetricNameVariable = "Arcus__ApplicationInsights__MetricName",
                             DatabricksUrlVariable = "Arcus__Databricks__Url";

        /// <summary>
        /// Gets the name of the Azure Function in the project.
        /// </summary>
        public const string FunctionName = "databricks-job-metrics";

        private AzureFunctionsDatabricksProject(
            TestConfig configuration, 
            ITestOutputHelper outputWriter) 
            : base(configuration.GetAzureFunctionsDatabricksJobMetricsProjectDirectory(), 
                   configuration, 
                   outputWriter)
        {
            AzureFunctionDatabricksConfig = configuration.GetDatabricksConfig();
        }

        /// <summary>
        /// Gets the Databricks connectivity information from the current application configuration, used by this project.
        /// </summary>
        public AzureFunctionDatabricksConfig AzureFunctionDatabricksConfig { get; }

        /// <summary>
        /// Starts a newly created project from the Azure Functions Databricks Job Metrics project template.
        /// </summary>
        /// <param name="configuration">The configuration to control the hosting of the to-be-created project.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A Azure Functions Databricks Job Metrics project with a full set of endpoint services to interact with the Azure Function.
        /// </returns>
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
            project.AddDatabricksSecurityToken(project.AzureFunctionDatabricksConfig.SecurityToken);
            project.AddStorageAccount();
            
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
                    .Replace("AddAzureKeyVaultWithManagedServiceIdentity(\"https://your-keyvault.vault.azure.net/\")", 
                             $"AddProvider(new {nameof(SingleValueSecretProvider)}(\"{securityToken}\"))"));
        }

        /// <summary>
        /// Customized project process preparation that results in an <see cref="ProcessStartInfo"/> instance.
        /// </summary>
        /// <param name="buildConfiguration">The configuration to which the project should built.</param>
        /// <param name="targetFramework">The code framework to which this project targets to.</param>
        /// <param name="commandArguments">The CLI parameters which should be sent to the starting project.</param>
        /// <returns>
        ///     An run-ready <see cref="ProcessStartInfo"/> instance that will be used to start the project.
        /// </returns>
        protected override ProcessStartInfo PrepareProjectRun(
            BuildConfiguration buildConfiguration,
            TargetFramework targetFramework,
            CommandArgument[] commandArguments)
        {
            ProcessStartInfo startInfo = base.PrepareProjectRun(buildConfiguration, targetFramework, commandArguments);
            Environment.SetEnvironmentVariable(ApplicationInsightsMetricNameVariable, ApplicationInsightsConfig.MetricName);
            Environment.SetEnvironmentVariable(DatabricksUrlVariable, AzureFunctionDatabricksConfig.BaseUrl);

            return startInfo;
        }

        /// <summary>
        /// Performs additional application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">The flag indicating whether or not the additional tasks should be disposed.</param>
        protected override void Disposing(bool disposing)
        {
            base.Disposing(disposing);
            Environment.SetEnvironmentVariable(ApplicationInsightsMetricNameVariable, null);
            Environment.SetEnvironmentVariable(DatabricksUrlVariable, null);
        }
    }
}
