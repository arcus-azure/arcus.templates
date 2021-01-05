using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.AzureFunctions.Http.Api;
using Arcus.Templates.Tests.Integration.Fixture;
using Flurl;
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
            OrderFunctionEndpoint = RootEndpoint.AppendPathSegments("order").ToUri();
            Order = new OrderService(OrderFunctionEndpoint, outputWriter);
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
        /// Starts a newly created project from the Azure Functions HTTP project template.
        /// </summary>
        /// <param name="configuration">The configuration to control the hosting of the to-be-created project.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A Azure Functions HTTP project with a full set of endpoint services to interact with the Azure Function.
        /// </returns>
        /// <exception cref="CannotStartTemplateProjectException">Thrown when the Azure Functions project cannot be started correctly.</exception>
        public static async Task<AzureFunctionsHttpProject> StartNewAsync(TestConfig configuration, ITestOutputHelper outputWriter)
        {
            AzureFunctionsHttpProject project = CreateNew(configuration, outputWriter);
            await project.StartAsync();

            return project;
        }

        private static AzureFunctionsHttpProject CreateNew(TestConfig configuration, ITestOutputHelper outputWriter)
        {
            var project = new AzureFunctionsHttpProject(configuration, outputWriter);
            project.CreateNewProject(new ProjectOptions());
            project.AddStorageAccount();
            
            return project;
        }

        private async Task StartAsync()
        {
            Run(BuildConfiguration.Debug, TargetFramework.NetCoreApp31);
            await WaitUntilTriggerIsAvailableAsync(OrderFunctionEndpoint);
        }
    }
}
