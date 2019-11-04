using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Health;
using Arcus.Templates.Tests.Integration.Swagger;
using GuardNet;
using Polly;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Fixture 
{
    /// <summary>
    /// Project template to create new web API projects.
    /// </summary>
    public class WebApiProject : TemplateProject
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        private WebApiProject(
            Uri baseUrl, 
            DirectoryInfo templateDirectory,
            DirectoryInfo fixturesDirectory,
            ITestOutputHelper outputWriter)
            : base(templateDirectory, fixturesDirectory, outputWriter)
        {
            Guard.NotNull(baseUrl, nameof(baseUrl), "Cannot create a web API services without a test configuration");

            Health = new HealthEndpointService(baseUrl, outputWriter);
            Swagger = new SwaggerEndpointService(baseUrl, outputWriter);
        }

        /// <summary>
        /// Starts a newly created project from the web API project template with a given set of <paramref name="projectOptions"/>.
        /// </summary>
        /// <param name="configuration">The configuration to control the hosting of the to-be-created project.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A web API project with a full set of endpoint services to interact with the API.
        /// </returns>
        public static async Task<WebApiProject> StartNewAsync(TestConfig configuration, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration), "Cannot create a web API project from the template without a test configuration");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Cannot create web API services without a test output logger");

            return await StartNewAsync(configuration, WebApiProjectOptions.Empty, outputWriter);
        }

        /// <summary>
        /// Starts a newly created project from the web API project template with a given set of <paramref name="projectOptions"/>.
        /// </summary>
        /// <param name="configuration">The configuration to control the hosting of the to-be-created project.</param>
        /// <param name="projectOptions">The project options to control the functionality of the to-be-created project from this template.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A web API project with a full set of endpoint services to interact with the API.
        /// </returns>
        public static async Task<WebApiProject> StartNewAsync(TestConfig configuration, WebApiProjectOptions projectOptions, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration), "Cannot create a web API project from the template without a test configuration");
            Guard.NotNull(projectOptions, nameof(projectOptions), "Cannot create a web API project without a set of project argument options");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Cannot create web API services without a test output logger");

            DirectoryInfo templateDirectory = configuration.GetWebApiProjectDirectory();
            DirectoryInfo fixtureDirectory = configuration.GetFixtureProjectDirectory();
            Uri baseUrl = configuration.CreateWebApiBaseUrl();
            var project = new WebApiProject(baseUrl, templateDirectory, fixtureDirectory, outputWriter);
            project.CreateNewProject(projectOptions);
            
            project.Run(configuration.BuildConfiguration, $"--ARCUS_HTTP_PORT {baseUrl.Port}");
            await WaitUntilWebProjectIsAvailable(baseUrl.Port, outputWriter);

            return project;
        }

        private static async Task WaitUntilWebProjectIsAvailable(int httpPort, ITestOutputHelper outputWriter)
        {
            var waitAndRetryForeverAsync =
                Policy.Handle<Exception>()
                      .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(1));

            var result = 
                await Policy.TimeoutAsync(TimeSpan.FromSeconds(10))
                            .WrapAsync(waitAndRetryForeverAsync)
                            .ExecuteAndCaptureAsync(() => GetNonExistingEndpoint(httpPort));

            if (result.Outcome == OutcomeType.Successful)
            {
                outputWriter.WriteLine("Test template web API project fully started at: localhost:{0}", httpPort);
            }
            else
            {
                outputWriter.WriteLine("Test template web API project could not be started");
                throw new CannotStartTemplateProject(
                    "The test project created from the web API project template doesn't seem to be running, "
                    + "please check any build or runtime errors that could occur when the test project was created");
            }
        }

        private static async Task<HttpStatusCode> GetNonExistingEndpoint(int httpPort)
        {
            using (HttpResponseMessage response = await HttpClient.GetAsync($"http://localhost:{httpPort}/not-exist"))
            {
                return response.StatusCode;
            }
        }

        /// <summary>
        /// Gets the service controlling the health information of the created web API project.
        /// </summary>
        public HealthEndpointService Health { get; }

        /// <summary>
        /// Gets the service controlling the Swagger information of the created web API project.
        /// </summary>
        public SwaggerEndpointService Swagger { get; }
    }
}