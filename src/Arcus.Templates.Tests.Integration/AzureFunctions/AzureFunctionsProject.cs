using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.AzureFunctions.Configuration;
using Arcus.Templates.Tests.Integration.AzureFunctions.Databricks.JobMetrics.Configuration;
using Arcus.Templates.Tests.Integration.Fixture;
using Flurl;
using GuardNet;
using Polly;
using Polly.Retry;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions
{
    /// <summary>
    /// Represents how an Azure Functions project template project should be tested.
    /// </summary>
    public abstract class AzureFunctionsProject : TemplateProject
    {
        protected const string ApplicationInsightsConnectionStringKeyVariable = "APPLICATIONINSIGHTS_CONNECTION_STRING";

        private static readonly HttpClient HttpClient = new HttpClient();

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionsProject"/> class.
        /// </summary>
        /// <param name="templateDirectory">The file directory where the .NET project template is located.</param>
        /// <param name="configuration">The configuration instance to retrieve Azure Functions-specific test values.</param>
        /// <param name="options">The options used to manipulate the resulting Azure Functions project.</param>
        /// <param name="outputWriter">The logger instance to write diagnostic trace messages during the lifetime of the test project.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="templateDirectory"/>, <paramref name="configuration"/>, or <paramref name="outputWriter"/> is <c>null</c>.</exception>
        protected  AzureFunctionsProject(
            DirectoryInfo templateDirectory, 
            TestConfig configuration,
            AzureFunctionsProjectOptions options,
            ITestOutputHelper outputWriter) 
            : base(templateDirectory, 
                   configuration.GetFixtureProjectDirectory(), 
                   outputWriter)
        {
            Guard.NotNull(templateDirectory, nameof(templateDirectory), "Requires a file template directory where the .NET project template is located");
            Guard.NotNull(configuration, nameof(configuration), "Requires an configuration instance to retrieve Azure Functions specific test values");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires an logger instance to write diagnostic trace messages during the lifetime of the project.");

            Configuration = configuration;
            FunctionsWorker = options.FunctionsWorker;
            RuntimeFileName = DetermineStartupCodeFileName();
            RootEndpoint = configuration.GenerateRandomLocalhostUrl().ResetToRoot().ToUri();
            AzureFunctionsConfig = configuration.GetAzureFunctionsConfig();
            ApplicationInsightsConfig = configuration.GetApplicationInsightsConfig();
        }

        /// <summary>
        /// Gets the Azure Functions worker type the project should target.
        /// </summary>
        public FunctionsWorker FunctionsWorker { get; }

        /// <summary>
        /// Gets the file name of the Azure Functions that contains the startup code ('Startup.cs' for in-process functions, 'Program.cs' for isolated functions).
        /// </summary>
        public string RuntimeFileName { get; }

        /// <summary>
        /// Gets the root endpoint on which the Azure Function is running.
        /// </summary>
        protected Uri RootEndpoint { get; }

        /// <summary>
        /// Gets the entire test configuration of the integration test suite to retrieve Azure Function-specific test values.
        /// </summary>
        protected TestConfig Configuration { get; }
        
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
        protected void AddLocalSettings()
        {
            string storageAccountConnectionString = AzureFunctionsConfig.StorageAccountConnectionString;
            string workerRuntime = DetermineWorkerRuntime();

            AddFileInProject("local.settings.json",  
                $"{{ \"IsEncrypted\": false, \"Values\": {{ \"AzureWebJobsStorage\": \"{storageAccountConnectionString}\", \"FUNCTIONS_WORKER_RUNTIME\": \"{workerRuntime}\", \"APPLICATIONINSIGHTS_CONNECTION_STRING\": \"\" }}, \"Host\": {{ \"LocalHttpPort\": {RootEndpoint.Port} }} }}");
        }

        private string DetermineWorkerRuntime()
        {
            switch (FunctionsWorker)
            {
                case FunctionsWorker.InProcess: return "dotnet";
                case FunctionsWorker.Isolated: return "dotnet-isolated";
                default:
                    throw new ArgumentOutOfRangeException(nameof(FunctionsWorker), FunctionsWorker, "Unknown Azure Functions worker type");
            }
        }

        private string DetermineStartupCodeFileName()
        {
            switch (FunctionsWorker)
            {
                case FunctionsWorker.InProcess: return "Startup.cs";
                case FunctionsWorker.Isolated: return "Program.cs";
                default:
                    throw new ArgumentOutOfRangeException(nameof(FunctionsWorker), FunctionsWorker, "Unknown Azure Functions worker type");
            }
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
            RunDotNet($"build -c {buildConfiguration} {ProjectDirectory.FullName}");

            string targetFrameworkIdentifier = GetTargetFrameworkIdentifier(targetFramework);
            var processInfo = new ProcessStartInfo("func", $"start --no-build --prefix bin/{buildConfiguration}/{targetFrameworkIdentifier}")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = ProjectDirectory.FullName,
            };
            Logger.WriteLine("> {0} {1}", processInfo.FileName, processInfo.Arguments);

            return processInfo;
        }

        /// <summary>
        /// Creates an user-friendly exception based on an occurred <paramref name="exception"/> to show and help the tester pinpoint the problem.
        /// </summary>
        /// <param name="exception">The occurred exception during the startup process of the test project based on the project template.</param>
        protected override CannotStartTemplateProjectException CreateProjectStartupFailure(Exception exception)
        {
            return new CannotStartTemplateProjectException(
                "Could start test project based on Azure Functions project template due to an exception occurred during the build/run process, "
                + "please check if the Azure Functions Core Tools (https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local) are installed and are available through the PATH environment variable, "
                + "or possible check for any compile errors or runtime failures (via the 'TearDownOptions') in the created test project based on the project template", exception);
        }

         /// <summary>
        /// Waits until the Azure Function project is fully running and ready to be interacted with.
        /// </summary>
        /// <param name="endpoint">The HTTP endpoint for the Azure Functions project to poll so the test project knows that the Azure Functions project is available.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="endpoint"/> is <c>null</c>.</exception>
        /// <exception cref="CannotStartTemplateProjectException">Thrown when the HTTP <paramref name="endpoint"/> was still not available after polling for some time.</exception>
        protected async Task WaitUntilTriggerIsAvailableAsync(Uri endpoint)
        {
            Guard.NotNull(endpoint, nameof(endpoint), "Requires an HTTP endpoint for the Azure Functions project so the project knows when the Azure Functions project is available");
            
            AsyncRetryPolicy retryPolicy =
                Policy.Handle<Exception>()
                      .WaitAndRetryForeverAsync(index => TimeSpan.FromMilliseconds(500));

            PolicyResult<HttpResponseMessage> result =
                await Policy.TimeoutAsync(TimeSpan.FromSeconds(30))
                            .WrapAsync(retryPolicy)
                            .ExecuteAndCaptureAsync(() => HttpClient.GetAsync(endpoint));

            if (result.Outcome == OutcomeType.Successful)
            {
                Logger.WriteLine("Test template Azure Functions project fully started at: {0}", endpoint);
            }
            else
            {
                Logger.WriteLine("Test template Azure Functions project could not be started at: {0}", endpoint);
                throw new CannotStartTemplateProjectException(
                    "The test project created from the Azure Functions project template doesn't seem to be running, "
                    + "please check any build or runtime errors that could occur when the test project was created");
            }
        }
    }
}
