using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.AzureFunctions.Http.Api;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.WebApi.Health;
using Arcus.Templates.Tests.Integration.WebApi.Swagger;
using Flurl;
using GuardNet;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Http
{
    /// <summary>
    /// Project template to create new Azure Functions HTTP projects.
    /// </summary>
    [DebuggerDisplay("Project = {ProjectDirectory.FullName}")]
    public class AzureFunctionsHttpProject : AzureFunctionsProject
    {
        /// <summary>
        /// Gets the name of the order Azure Function.
        /// </summary>
        public const string OrderFunctionName = "order";
        
        private AzureFunctionsHttpProject(
            TestConfig configuration, 
            ITestOutputHelper outputWriter) 
            : base(configuration.GetAzureFunctionsHttpProjectDirectory(), 
                   configuration, 
                   outputWriter)
        {
            OrderFunctionEndpoint = RootEndpoint.AppendPathSegments("api", "v1", "order").ToUri();
            Order = new OrderService(OrderFunctionEndpoint, outputWriter);
            Health = new HealthEndpointService(
                RootEndpoint.AppendPathSegments("api", "v1").ToUri(), 
                outputWriter);
            Swagger = new SwaggerEndpointService(
                RootEndpoint.AppendPathSegments("api", "swagger", "ui"),
                RootEndpoint.AppendPathSegments("api", "swagger.json"),
                outputWriter);
        }
        
        /// <summary>
        /// Gets the endpoint of the order Azure Function.
        /// </summary>
        public Uri OrderFunctionEndpoint { get; }
        
        /// <summary>
        /// Gets the service to interact with the order functionality of the Azure Function.
        /// </summary>
        public OrderService Order { get; }

        /// <summary>
        /// Gets the service to interact with the health checks functionality of the Azure Function.
        /// </summary>
        public HealthEndpointService Health { get; }
        
        /// <summary>
        /// Gets the service to interact with the Swagger OpenAPI documentation of the Azure Function.
        /// </summary>
        public SwaggerEndpointService Swagger { get; }

        /// <summary>
        /// Starts a newly created project from the Azure Functions HTTP project template.
        /// </summary>
        /// <param name="outputWriter">The output logger to write telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     An Azure Functions HTTP project with a full set of endpoint services to interact with.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="outputWriter"/> is <c>null</c>.</exception>
        /// <exception cref="CannotStartTemplateProjectException">Thrown when the Azure Functions project cannot be started correctly.</exception>
        public static async Task<AzureFunctionsHttpProject> StartNewAsync(ITestOutputHelper outputWriter)
        {
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to write diagnostic information during the creation and startup process");

            AzureFunctionsHttpProject project = await StartNewAsync(new AzureFunctionsHttpProjectOptions(), outputWriter);
            return project;
        }
        
        /// <summary>
        /// Starts a newly created project from the Azure Functions HTTP project template.
        /// </summary>
        /// <param name="options">The project options to control the functionality of the to-be-created project from this template.</param>
        /// <param name="outputWriter">The output logger to write telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     An Azure Functions HTTP project with a full set of endpoint services to interact with.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/>, or <paramref name="outputWriter"/> is <c>null</c>.</exception>
        /// <exception cref="CannotStartTemplateProjectException">Thrown when the Azure Functions project cannot be started correctly.</exception>
        public static async Task<AzureFunctionsHttpProject> StartNewAsync(
            AzureFunctionsHttpProjectOptions options,
            ITestOutputHelper outputWriter)
        {
            Guard.NotNull(options, nameof(options), "Requires a set of project argument options to create the Azure Functions HTTP trigger project");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to write diagnostic information during the creation and startup process");

            AzureFunctionsHttpProject project = await StartNewAsync(TestConfig.Create(), options, outputWriter);
            return project;
        }
        
        /// <summary>
        /// Starts a newly created project from the Azure Functions HTTP project template.
        /// </summary>
        /// <param name="configuration">The configuration to control the hosting of the to-be-created project.</param>
        /// <param name="outputWriter">The output logger to write telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     An Azure Functions HTTP project with a full set of endpoint services to interact with.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuration"/> or <paramref name="outputWriter"/> is <c>null</c>.</exception>
        /// <exception cref="CannotStartTemplateProjectException">Thrown when the Azure Functions project cannot be started correctly.</exception>
        public static async Task<AzureFunctionsHttpProject> StartNewAsync(TestConfig configuration, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a configuration instance to control the hosting of the to-be-created project");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to write diagnostic information during the creation and startup process");

            AzureFunctionsHttpProject project = await StartNewAsync(configuration, new AzureFunctionsHttpProjectOptions(), outputWriter);
            return project;
        }

        /// <summary>
        /// Starts a newly created project from the Azure Functions HTTP project template.
        /// </summary>
        /// <param name="configuration">The configuration to control the hosting of the to-be-created project.</param>
        /// <param name="options">The project options to control the functionality of the to-be-created project from this template.</param>
        /// <param name="outputWriter">The output logger to write telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     An Azure Functions HTTP project with a full set of endpoint services to interact with.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="configuration"/>, <paramref name="options"/>, or <paramref name="outputWriter"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="CannotStartTemplateProjectException">Thrown when the Azure Functions project cannot be started correctly.</exception>
        public static async Task<AzureFunctionsHttpProject> StartNewAsync(
            TestConfig configuration,
            AzureFunctionsHttpProjectOptions options,
            ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a configuration instance to control the hosting of the to-be-created project");
            Guard.NotNull(options, nameof(options), "Requires a set of project argument options to create the Azure Functions HTTP trigger project");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to write diagnostic information during the creation and startup process");

            AzureFunctionsHttpProject project = CreateNew(configuration, options, outputWriter);
            await project.StartAsync();

            return project;
        }

        /// <summary>
        /// Creates a new temporary project based on the Azure Functions HTTP trigger project template.
        /// </summary>
        /// <param name="configuration">The configuration to control the hosting of the to-be-created project.</param>
        /// <param name="outputWriter">The output logger to write telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     An Azure Functions HTTP trigger project with a full set of endpoint services to interact with.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuration"/> or <paramref name="outputWriter"/> is <c>null</c>.</exception>
        public static AzureFunctionsHttpProject CreateNew(TestConfig configuration, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a configuration instance to control the hosting of the to-be-created project");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to write diagnostic information during the creation and startup process");

            AzureFunctionsHttpProject project = CreateNew(configuration, new AzureFunctionsHttpProjectOptions(), outputWriter);
            return project;
        }

        /// <summary>
        /// Creates a new temporary project based on the Azure Functions HTTP trigger project template.
        /// </summary>
        /// <param name="configuration">The configuration to control the hosting of the to-be-created project.</param>
        /// <param name="options">The project options to control the functionality of the to-be-created project from this template.</param>
        /// <param name="outputWriter">The output logger to write telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     An Azure Functions HTTP trigger project with a full set of endpoint services to interact with.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="configuration"/>, <paramref name="options"/> or <paramref name="outputWriter"/> is <c>null</c>.
        /// </exception>
        public static AzureFunctionsHttpProject CreateNew(TestConfig configuration, AzureFunctionsHttpProjectOptions options, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a configuration instance to control the hosting of the to-be-created project");
            Guard.NotNull(options, nameof(options), "Requires a set of project argument options to create the Azure Functions HTTP trigger project");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to write diagnostic information during the creation and startup process");
            
            var project = new AzureFunctionsHttpProject(configuration, outputWriter);
            project.CreateNewProject(options);
            project.AddStorageAccount();
            
            return project;
        }

        /// <summary>
        /// Starts the Azure Functions HTTP trigger project, created from the template.
        /// </summary>
        public async Task StartAsync()
        {
            Run(Configuration.BuildConfiguration, TargetFramework.NetCoreApp31);
            await WaitUntilTriggerIsAvailableAsync(OrderFunctionEndpoint);
        }
    }
}
