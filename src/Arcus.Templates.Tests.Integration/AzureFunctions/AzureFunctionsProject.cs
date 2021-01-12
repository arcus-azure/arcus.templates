using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.AzureFunctions.Configuration;
using Arcus.Templates.Tests.Integration.AzureFunctions.Databricks.JobMetrics.Configuration;
using Arcus.Templates.Tests.Integration.Fixture;
using GuardNet;
using Newtonsoft.Json.Linq;
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
        protected const string ApplicationInsightsInstrumentationKeyVariable = "APPLICATIONINSIGHTS_INSTRUMENTATIONKEY";

        private static readonly HttpClient HttpClient = new HttpClient();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionsProject"/> class.
        /// </summary>
        /// <param name="templateDirectory">The file directory where the .NET project template is located.</param>
        /// <param name="configuration">The configuration instance to retrieve Azure Functions specific test values.</param>
        /// <param name="outputWriter">The logger instance to write diagnostic trace messages during the lifetime of the test project.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="templateDirectory"/>, <paramref name="configuration"/>, or <paramref name="outputWriter"/> is <c>null</c>.</exception>
        protected  AzureFunctionsProject(
            DirectoryInfo templateDirectory, 
            TestConfig configuration,
            ITestOutputHelper outputWriter) 
            : base(templateDirectory, 
                   configuration.GetFixtureProjectDirectory(), 
                   outputWriter)
        {
            Guard.NotNull(templateDirectory, nameof(templateDirectory), "Requires a file template directory where the .NET project template is located");
            Guard.NotNull(configuration, nameof(configuration), "Requires an configuration instance to retrieve Azure Functions specific test values");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires an logger instance to write diagnostic trace messages during the lifetime of the project.");

            Configuration = configuration;
            RootEndpoint = configuration.GenerateRandomLocalhostUrl();
            AzureFunctionsConfig = configuration.GetAzureFunctionsConfig();
            ApplicationInsightsConfig = configuration.GetApplicationInsightsConfig();
        }

        /// <summary>
        /// Gets the root endpoint on which the Azure Function is running.
        /// </summary>
        protected Uri RootEndpoint { get; }

        /// <summary>
        /// Gets the Azure Functions information from the current application configuration, used by this project.
        /// </summary>
        protected AzureFunctionsConfig AzureFunctionsConfig { get; }

        /// <summary>
        /// Gets the Application Insights connectivity information from the current application configuration, used  by this project.
        /// </summary>
        protected  ApplicationInsightsConfig ApplicationInsightsConfig { get; }

        /// <summary>
        /// Gets the test configuration that contains the current application configuration, used by this project.
        /// </summary>
        protected TestConfig Configuration { get; }
        
        /// <summary>
        /// Adds an test Azure storage account connection string to the Azure Function project so the project can start up correctly.
        /// </summary>
        protected void AddStorageAccount()
        {
            string storageAccountConnectionString = AzureFunctionsConfig.StorageAccountConnectionString;
            
            AddFileInProject("local.settings.json", 
                $"{{ " +
                    $"\"IsEncrypted\": false, " +
                    $"\"Values\": {{ \"AzureWebJobsStorage\": \"{storageAccountConnectionString}\", \"FUNCTIONS_WORKER_RUNTIME\": \"dotnet\" }}, " +
                    $"\"Host\": {{ \"LocalHttpPort\": {RootEndpoint.Port} }}" +
                $"}}");
        }
        
        /// <summary>
        /// Adds a local key/value setting to the 'local.settings.json' file of the Azure Functions application.
        /// </summary>
        /// <param name="name">The name of the new application setting.</param>
        /// <param name="value">The value of the application setting.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="name"/> is blank.</exception>
        /// <exception cref="FileNotFoundException">Thrown when no 'local.settings.json' file can be found on the Azure Functions test project.</exception>
        /// <exception cref="JsonException">Thrown when the 'local.settings.json' file doesn't contain the necessary JSON tokens to add the new application setting.</exception>
        protected void AddLocalSetting(string name, string value)
        {
            Guard.NotNullOrWhitespace(name, nameof(name), "Requires a non-blank name for the local setting to be added to the 'local.settings.json' file");
            
            if (!File.Exists(Path.Combine(ProjectDirectory.FullName, "local.settings.json")))
            {
                throw new FileNotFoundException(
                    "Cannot find 'local.settings.json' file in project directory. Make sure that such a file is present before trying to add local settings");
            }
            
            UpdateFileInProject("local.settings.json",
                contents =>
                {
                    JObject json = JObject.Parse(contents);
                    JToken values = json["Values"];
                    if (values is null)
                    {
                        throw new JsonException("Cannot add a local setting to the 'local.settings.json' file because the JSON 'Values' token is not present");
                    }
                    
                    values[name] = value;

                    return json.ToString();
                });
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
                Logger.WriteLine("Test template Azure Functions project could not be started");
                throw new CannotStartTemplateProjectException(
                    "The test project created from the Azure Functions project template doesn't seem to be running, "
                    + "please check any build or runtime errors that could occur when the test project was created");
            }
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
