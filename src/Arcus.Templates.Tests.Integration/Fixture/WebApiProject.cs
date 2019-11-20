using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Health;
using Arcus.Templates.Tests.Integration.Swagger;
using Flurl;
using GuardNet;
using Polly;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Fixture 
{
    /// <summary>
    /// Project template to create new web API projects.
    /// </summary>
    [DebuggerDisplay("Project = {ProjectDirectory.FullName}, URL = {_baseUrl}")]
    public class WebApiProject : TemplateProject
    {
        private readonly Uri _baseUrl;
        private readonly TestConfig _configuration;
        private readonly ITestOutputHelper _outputWriter;

        private static readonly HttpClient HttpClient = new HttpClient();

        private WebApiProject(
            Uri baseUrl, 
            TestConfig configuration,
            DirectoryInfo templateDirectory,
            DirectoryInfo fixturesDirectory,
            ITestOutputHelper outputWriter)
            : base(templateDirectory, fixturesDirectory, outputWriter)
        {
            Guard.NotNull(baseUrl, nameof(baseUrl), "Cannot create a web API services without a test configuration");
            Guard.NotNull(configuration, nameof(configuration), "Cannot create a web API project without a configuration that describes startup settings");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Cannot create a web API project without a test logger that sends diagnostic messages during the creation of the project");

            _baseUrl = baseUrl;
            _configuration = configuration;
            _outputWriter = outputWriter; 

            Health = new HealthEndpointService(baseUrl, outputWriter);
            Swagger = new SwaggerEndpointService(baseUrl, outputWriter);
            Root = new RootEndpointService(baseUrl, outputWriter);
        }

        /// <summary>
        /// Starts a newly created project from the web API project template without a given set of project options.
        /// </summary>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A web API project with a full set of endpoint services to interact with the API.
        /// </returns>
        public static async Task<WebApiProject> StartNewAsync(ITestOutputHelper outputWriter)
        {
            Guard.NotNull(outputWriter, nameof(outputWriter), "Cannot create web API services without a test output logger");

            return await StartNewAsync(TestConfig.Create(), outputWriter);
        }

        /// <summary>
        /// Starts a newly created project from the web API project template without a given set of project options.
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
        /// <param name="projectOptions">The project options to control the functionality of the to-be-created project from this template.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A web API project with a full set of endpoint services to interact with the API.
        /// </returns>
        public static async Task<WebApiProject> StartNewAsync(WebApiProjectOptions projectOptions, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(projectOptions, nameof(projectOptions), "Cannot create a web API project without a set of project argument options");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Cannot create web API services without a test output logger");

            return await StartNewAsync(TestConfig.Create(), projectOptions, outputWriter);
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

            WebApiProject project = CreateNew(configuration, projectOptions, outputWriter);
            await project.StartAsync();

            return project;
        }

        /// <summary>
        /// Creates a project from the web API project template without a set project options.
        /// </summary>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A not yet started web API project with a full set of endpoint services to interact with the API.
        /// </returns>
        /// <remarks>
        ///     Before the project can be interacted with, the project must be started by calling the <see cref="StartAsync" /> method.
        /// </remarks>
        public static WebApiProject CreateNew(ITestOutputHelper outputWriter)
        {
            Guard.NotNull(outputWriter, nameof(outputWriter), "Cannot create web API services without a test output logger");

            return CreateNew(TestConfig.Create(), WebApiProjectOptions.Empty, outputWriter);
        }

        /// <summary>
        /// Creates a project from the web API project template without a set of project options.
        /// </summary>
        /// <param name="configuration">The configuration to control the hosting of the to-be-created project.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A not yet started web API project with a full set of endpoint services to interact with the API.
        /// </returns>
        /// <remarks>
        ///     Before the project can be interacted with, the project must be started by calling the <see cref="StartAsync" /> method.
        /// </remarks>
        public static WebApiProject CreateNew(TestConfig configuration, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration), "Cannot create a web API project from the template without a test configuration");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Cannot create web API services without a test output logger");

            return CreateNew(configuration, WebApiProjectOptions.Empty, outputWriter);
        }

        /// <summary>
        /// Creates a project from the web API project template with a set of <paramref name="projectOptions"/>.
        /// </summary>
        /// <param name="projectOptions">The project options to control the functionality of the to-be-created project from this template.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A not yet started web API project with a full set of endpoint services to interact with the API.
        /// </returns>
        /// <remarks>
        ///     Before the project can be interacted with, the project must be started by calling the <see cref="StartAsync" /> method.
        /// </remarks>
        public static WebApiProject CreateNew(WebApiProjectOptions projectOptions, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(projectOptions, nameof(projectOptions), "Cannot create a web API project without a set of project argument options");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Cannot create web API services without a test output logger");

            return CreateNew(TestConfig.Create(), projectOptions, outputWriter);
        }

        /// <summary>
        /// Creates a project from the web API project template with a given set of <paramref name="projectOptions"/>.
        /// </summary>
        /// <param name="configuration">The configuration to control the hosting of the to-be-created project.</param>
        /// <param name="projectOptions">The project options to control the functionality of the to-be-created project from this template.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A not yet started web API project with a full set of endpoint services to interact with the API.
        /// </returns>
        /// <remarks>
        ///     Before the project can be interacted with, the project must be started by calling the <see cref="StartAsync" /> method.
        /// </remarks>
        public static WebApiProject CreateNew(TestConfig configuration, WebApiProjectOptions projectOptions, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration), "Cannot create a web API project from the template without a test configuration");
            Guard.NotNull(projectOptions, nameof(projectOptions), "Cannot create a web API project without a set of project argument options");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Cannot create web API services without a test output logger");

            DirectoryInfo templateDirectory = configuration.GetWebApiProjectDirectory();
            DirectoryInfo fixtureDirectory = configuration.GetFixtureProjectDirectory();
            Uri baseUrl = configuration.CreateWebApiBaseUrl();
            var project = new WebApiProject(baseUrl, configuration, templateDirectory, fixtureDirectory, outputWriter);
            project.CreateNewProject(projectOptions);

            return project;
        }

        /// <summary>
        /// Starts the web API project on a previously configured endpoint.
        /// </summary>
        public async Task StartAsync()
        {
            Run(_configuration.BuildConfiguration, $"--ARCUS_HTTP_PORT {_baseUrl.Port}");
            await WaitUntilWebProjectIsAvailable(_baseUrl.Port);
        }

        private async Task WaitUntilWebProjectIsAvailable(int httpPort)
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
                _outputWriter.WriteLine("Test template web API project fully started at: localhost:{0}", httpPort);
            }
            else
            {
                _outputWriter.WriteLine("Test template web API project could not be started");
                throw new CannotStartTemplateProjectException(
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
        /// Gets the service that exposes default HTTP operations to interact on a lower level with the created web API project.
        /// </summary>
        public RootEndpointService Root { get; }

        /// <summary>
        /// Gets the service controlling the health information of the created web API project.
        /// </summary>
        public HealthEndpointService Health { get; }

        /// <summary>
        /// Gets the service controlling the Swagger information of the created web API project.
        /// </summary>
        public SwaggerEndpointService Swagger { get; }

        /// <summary>
        /// Updates a file in the target project folder, using the given <paramref name="updateContents"/> function.
        /// </summary>
        /// <param name="fileName">The target file name to change it's contents.</param>
        /// <param name="updateContents">The function that changes the contents of the file.</param>
        public void UpdateFileInProject(string fileName, Func<string, string> updateContents)
        {
            Guard.NotNull(fileName, nameof(fileName), "Requires a file name (no file path) to update the contents");
            Guard.NotNull(updateContents, nameof(updateContents), "Requires a function to update the project file contents");

            string destPath = Path.Combine(ProjectDirectory.FullName, fileName);
            if (!File.Exists(destPath))
            {
                throw new FileNotFoundException($"No project file with the file name: '{fileName}' was found in the target project folder");
            }

            string content = File.ReadAllText(destPath);
            content = updateContents(content);
            File.WriteAllText(destPath, content);
        }

        /// <summary>
        /// Adds a fixture file to the web API project by its type: <typeparamref name="TFixture"/>,
        /// and replace tokens with values via the given <paramref name="replacements"/> dictionary.
        /// </summary>
        /// <typeparam name="TFixture">The fixture type to include in the web API project.</typeparam>
        /// <param name="replacements">The tokens and their corresponding values to replace in the fixture file.</param>
        /// <param name="namespaces">The additional namespace the fixture file should be placed in.</param>
        public void AddFixture<TFixture>(IDictionary<string, string> replacements = null, params string[] namespaces)
        {
            replacements = replacements ?? new Dictionary<string, string>();
            namespaces = namespaces ?? new string[0];

            string srcPath = FindFixtureTypeInDirectory(FixtureDirectory, typeof(TFixture));
            string destPath = Path.Combine(ProjectDirectory.FullName, Path.Combine(namespaces), typeof(TFixture).Name + ".cs");
            File.Copy(srcPath, destPath);

            string key = typeof(TFixture).Namespace ?? throw new InvalidOperationException("Generic fixture requires a namespace");
            string value = $"{ProjectName}.{String.Join(".", namespaces)}";
            replacements[key] = value;

            string content = File.ReadAllText(destPath);
            content = replacements.Aggregate(content, (txt, kv) => txt.Replace(kv.Key, kv.Value));
            
            File.WriteAllText(destPath, content);
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

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"Project: {ProjectDirectory.FullName}, running at: {_baseUrl.OriginalString.ResetToRoot()}";
        }
    }
}