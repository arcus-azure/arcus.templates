using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using GuardNet;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Timer
{
    /// <summary>
    /// Project template to create new Azure Functions Timer projects.
    /// </summary>
    [DebuggerDisplay("Project = {ProjectDirectory.FullName}")]
    public class AzureFunctionsTimerProject : AzureFunctionsProject
    {
        private AzureFunctionsTimerProject(
            TestConfig configuration, 
            AzureFunctionsProjectOptions options, 
            ITestOutputHelper outputWriter) 
            : base(configuration.GetAzureFunctionsTimerProjectDirectory(), 
                   configuration, 
                   options, 
                   outputWriter)
        {
        }

        /// <summary>
        /// Starts a newly created project from the Azure Functions Timer project template.
        /// </summary>
        /// <param name="outputWriter">The output logger to write telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     An Azure Functions HTTP project with a full set of endpoint services to interact with.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="outputWriter"/> is <c>null</c>.</exception>
        /// <exception cref="CannotStartTemplateProjectException">Thrown when the Azure Functions project cannot be started correctly.</exception>
        public static async Task<AzureFunctionsTimerProject> StartNewAsync(ITestOutputHelper outputWriter)
        {
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to write diagnostic information during the creation and startup process");

            return await StartNewAsync(new AzureFunctionsProjectOptions(), outputWriter);
        }

        /// <summary>
        /// Starts a newly created project from the Azure Functions Timer project template.
        /// </summary>
        /// <param name="options">The project options to control the functionality of the to-be-created project from this template.</param>
        /// <param name="outputWriter">The output logger to write telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     An Azure Functions HTTP project with a full set of endpoint services to interact with.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="outputWriter"/> is <c>null</c>.</exception>
        /// <exception cref="CannotStartTemplateProjectException">Thrown when the Azure Functions project cannot be started correctly.</exception>
        public static async Task<AzureFunctionsTimerProject> StartNewAsync(AzureFunctionsProjectOptions options, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to write diagnostic information during the creation and startup process");

            var config = TestConfig.Create();
            var project = new AzureFunctionsTimerProject(config, options, outputWriter);

            project.CreateNewProject(options);
            project.AddLocalSettings();
            project.UpdateFileInProject(project.RuntimeFileName, contents => project.RemovesUserErrorsFromContents(contents));

            await project.StartAsync();

            return project;
        }

        /// <summary>
        /// Starts the Azure Functions Timer trigger project, created from the template.
        /// </summary>
        private async Task StartAsync()
        {
            try
            {
                Run(Configuration.BuildConfiguration, TargetFramework.Net6_0);
                await WaitUntilTriggerIsAvailableAsync(RootEndpoint);
            }
            catch
            {
                Dispose();
                throw;
            }
        }
    }
}
