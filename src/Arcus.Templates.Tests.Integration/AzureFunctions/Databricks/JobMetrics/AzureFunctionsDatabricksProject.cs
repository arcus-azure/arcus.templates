using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.AzureFunctions.Admin;
using Arcus.Templates.Tests.Integration.AzureFunctions.Databricks.JobMetrics.Configuration;
using Arcus.Templates.Tests.Integration.Fixture;
using GuardNet;
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
            AzureFunctionsDatabricksProjectOptions options,
            ITestOutputHelper outputWriter) 
            : base(configuration.GetAzureFunctionsDatabricksJobMetricsProjectDirectory(), 
                   configuration, 
                   options,
                   outputWriter)
        {
            AzureFunctionDatabricksConfig = configuration.GetDatabricksConfig();
            Admin = new AdminEndpointService(RootEndpoint.Port, FunctionName, outputWriter);
        }

        /// <summary>
        /// Gets the Databricks connectivity information from the current application configuration, used by this project.
        /// </summary>
        public AzureFunctionDatabricksConfig AzureFunctionDatabricksConfig { get; }

        /// <summary>
        /// Gets the service to run administrative actions on the Azure Functions project.
        /// </summary>
        public AdminEndpointService Admin { get; }

        /// <summary>
        /// Starts a newly created project from the Azure Functions Databricks Job Metrics project template.
        /// </summary>
        /// <param name="configuration">The configuration to control the hosting of the to-be-created project.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A Azure Functions Databricks Job Metrics project with a full set of endpoint services to interact with the Azure Function.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuration"/> or <paramref name="outputWriter"/> is <c>null</c>.</exception>
        public static async Task<AzureFunctionsDatabricksProject> StartNewAsync(TestConfig configuration, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a test configuration instance to retrieve integration test configuration values to interact with Azure Databricks");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a logging instance to write diagnostic information during the creation and startup process");

            AzureFunctionsDatabricksProject project = await StartNewAsync(configuration, new AzureFunctionsDatabricksProjectOptions(), outputWriter);
            return project;
        }

        /// <summary>
        /// Starts a newly created project from the Azure Functions Databricks Job Metrics project template.
        /// </summary>
        /// <param name="configuration">The configuration to control the hosting of the to-be-created project.</param>
        /// <param name="options">The additional user project options to change the project contents and functionality.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A Azure Functions Databricks Job Metrics project with a full set of endpoint services to interact with the Azure Function.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="configuration"/>, <paramref name="options"/>, or <paramref name="outputWriter"/> is <c>null</c>.
        /// </exception>
        public static async Task<AzureFunctionsDatabricksProject> StartNewAsync(TestConfig configuration, AzureFunctionsDatabricksProjectOptions options, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a test configuration instance to retrieve integration test configuration values to interact with Azure Databricks");
            Guard.NotNull(options, nameof(options), "Requires a project options to change the project contents and functionality");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a logging instance to write diagnostic information during the creation and startup process");

            AzureFunctionsDatabricksProject project = CreateNew(configuration, options, outputWriter);
            await project.StartAsync();

            return project;
        }

        /// <summary>
        /// Creates a project from the Azure Functions Databricks Job Metrics project template.
        /// </summary>
        /// <param name="configuration">The configuration to control the hosting of the to-be-created project.</param>
        /// <param name="options">The additional user project options to change the project contents and functionality.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation process.</param>
        /// <returns>
        ///     A Azure Functions Databricks Job Metrics project with a full set of endpoint services to interact with the Azure Function.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="configuration"/>, <paramref name="options"/>, or <paramref name="outputWriter"/> is <c>null</c>.
        /// </exception>
        public static AzureFunctionsDatabricksProject CreateNew(TestConfig configuration, AzureFunctionsDatabricksProjectOptions options, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a test configuration instance to retrieve integration test configuration values to interact with Azure Databricks");
            Guard.NotNull(options, nameof(options), "Requires a project options to change the project contents and functionality");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a logging instance to write diagnostic information during the creation process");

            var project = new AzureFunctionsDatabricksProject(configuration, options, outputWriter);
            project.CreateNewProject(options);
            project.AddDatabricksSecurityToken(project.AzureFunctionDatabricksConfig.SecurityToken);
            project.AddLocalSettings();
            
            return project;
        }

        private async Task StartAsync()
        {
            try
            {
                Run(Configuration.BuildConfiguration, TargetFramework.Net6_0);
                await WaitUntilTriggerIsAvailableAsync(Admin.Endpoint);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void AddDatabricksSecurityToken(string securityToken)
        {
            AddTypeAsFile<SingleValueSecretProvider>();

            UpdateFileInProject(RuntimeFileName, contents =>
                RemovesUserErrorsFromContents(contents)
                    .Replace("AddAzureKeyVaultWithManagedIdentity(\"https://your-keyvault.vault.azure.net/\", CacheConfiguration.Default)", 
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

            ApplicationInsightsConfig appInsightsConfig = Configuration.GetApplicationInsightsConfig();
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING", $"InstrumentationKey={appInsightsConfig.InstrumentationKey}");

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
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING", null);
        }
    }
}
