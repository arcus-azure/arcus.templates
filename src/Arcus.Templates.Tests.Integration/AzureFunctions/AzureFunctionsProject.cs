using System;
using System.Diagnostics;
using System.IO;
using Arcus.Templates.Tests.Integration.AzureFunctions.Databricks.JobMetrics.Configuration;
using Arcus.Templates.Tests.Integration.Fixture;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions
{
    /// <summary>
    /// Represents how an Azure Functions project template project should be tested.
    /// </summary>
    public abstract class AzureFunctionsProject : TemplateProject
    {
        protected const string ApplicationInsightsInstrumentationKeyVariable = "APPLICATIONINSIGHTS_INSTRUMENTATIONKEY";

        protected  AzureFunctionsProject(
            DirectoryInfo templateDirectory, 
            TestConfig configuration,
            ITestOutputHelper outputWriter) 
            : base(templateDirectory, 
                   configuration.GetFixtureProjectDirectory(), 
                   outputWriter)
        {
            AzureFunctionsConfig = configuration.GetAzureFunctionsConfig();
            ApplicationInsightsConfig = configuration.GetApplicationInsightsConfig();
        }

        /// <summary>
        /// Gets the Azure Functions information from the current application configuration, used by this project.
        /// </summary>
        protected AzureFunctionsConfig AzureFunctionsConfig { get; }

        /// <summary>
        /// Gets the Application Insights connectivity information from the current application configuration, used  by this project.
        /// </summary>
        protected  ApplicationInsightsConfig ApplicationInsightsConfig { get; }

        /// <summary>
        /// Adds an test Azure storage account connection string to the Azure Function project so the project can start up correctly.
        /// </summary>
        protected void AddStorageAccount()
        {
            string storageAccountConnectionString = AzureFunctionsConfig.StorageAccountConnectionString;
            AddFileInProject("local.settings.json", 
                $"{{ \"IsEncrypted\": false, \"Values\": {{ \"AzureWebJobsStorage\": \"{storageAccountConnectionString}\", \"FUNCTIONS_WORKER_RUNTIME\": \"dotnet\" }} }}");
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
            RunDotNet($"build {ProjectDirectory.FullName}");

            var processInfo = new ProcessStartInfo("func", "start")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = ProjectDirectory.FullName,
            };

            Environment.SetEnvironmentVariable(ApplicationInsightsInstrumentationKeyVariable, ApplicationInsightsConfig.InstrumentationKey);
            return processInfo;
        }

        /// <summary>
        /// Performs additional application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">The flag indicating whether or not the additional tasks should be disposed.</param>
        protected override void Disposing(bool disposing)
        {
            Environment.SetEnvironmentVariable(ApplicationInsightsInstrumentationKeyVariable, null);
        }
    }
}
