using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.AzureFunctions.Admin;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Worker.Fixture;
using Arcus.Templates.Tests.Integration.Worker.ServiceBus.Fixture;
using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using GuardNet;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.ServiceBus
{
    /// <summary>
    /// Project template to create new Azure Functions Service Bus projects.
    /// </summary>
    [DebuggerDisplay("Project = {ProjectDirectory.FullName}")]
    public class AzureFunctionsServiceBusProject : AzureFunctionsProject
    {
        private AzureFunctionsServiceBusProject(
            ServiceBusEntityType entityType, 
            TestConfig configuration, 
            AzureFunctionsServiceBusProjectOptions options,
            ITestOutputHelper outputWriter) 
            : base(configuration.GetAzureFunctionsServiceBusProjectDirectory(entityType), 
                   configuration, 
                   options,
                   outputWriter)
        {
            Messaging = new TestServiceBusMessagePumpService(entityType, configuration, ProjectDirectory, outputWriter);
            Admin = new AdminEndpointService(RootEndpoint.Port, "order-processing", outputWriter);
        }

        /// <summary>
        /// Gets the service that interacts with the hosted-service message pump in the Azure Functions Service Bus project.
        /// </summary>
        /// <remarks>
        ///     Only when the project is started, is this service available for interaction.
        /// </remarks>
        public IMessagingService Messaging { get; }

        /// <summary>
        /// Gets the service to run administrative actions on the Azure Functions project.
        /// </summary>
        public AdminEndpointService Admin { get; }

        /// <summary>
        /// Starts a newly created project from the Azure Functions Service Bus project template.
        /// </summary>
        /// <param name="entityType">The type of the Azure Service Bus entity, to control the used project template.</param>
        /// <param name="configuration">The collection of configuration values to correctly initialize the resulting project with secret values.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     An Azure Functions Service Bus project with a set of services to interact with the worker.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="configuration"/>, or the <paramref name="outputWriter"/> is <c>null</c>.
        /// </exception>
        public static async Task<AzureFunctionsServiceBusProject> StartNewProjectAsync(
            ServiceBusEntityType entityType,
            TestConfig configuration,
            ITestOutputHelper outputWriter)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a configuration instance to retrieve the configuration values to pass along to the to-be-created project");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to write diagnostic information during the creation and startup process");

            return await StartNewProjectAsync(entityType, new AzureFunctionsServiceBusProjectOptions(), configuration, outputWriter);
        }

        /// <summary>
        /// Starts a newly created project from the Azure Functions Service Bus project template.
        /// </summary>
        /// <param name="entityType">The type of the Azure Service Bus entity, to control the used project template.</param>
        /// <param name="options">The additional project options to pass along to the project creation command.</param>
        /// <param name="configuration">The collection of configuration values to correctly initialize the resulting project with secret values.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     An Azure Functions Service Bus project with a set of services to interact with the project.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="options"/>, the <paramref name="configuration"/>, or the <paramref name="outputWriter"/> is <c>null</c>.
        /// </exception>
        public static async Task<AzureFunctionsServiceBusProject> StartNewProjectAsync(
            ServiceBusEntityType entityType,
            AzureFunctionsServiceBusProjectOptions options,
            TestConfig configuration,
            ITestOutputHelper outputWriter)
        {
            Guard.NotNull(options, nameof(options), "Requires a set of project options to pass along to the project creation command");
            Guard.NotNull(configuration, nameof(configuration), "Requires a configuration instance to retrieve the configuration values to pass along to the to-be-created project");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to write diagnostic information during the creation and startup process");

            AzureFunctionsServiceBusProject project = CreateNew(entityType, options, configuration, outputWriter);

            await project.StartAsync(entityType);
            return project;
        }

        /// <summary>
        /// Creates a new project from the Azure Functions Service Bus project template.
        /// </summary>
        /// <param name="entityType">The type of the Azure Service Bus entity, to control the used project template.</param>
        /// <param name="options">The additional project options to pass along to the project creation command.</param>
        /// <param name="configuration">The collection of configuration values to correctly initialize the resulting project with secret values.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation process.</param>
        /// <returns>
        ///     An Azure Functions Service Bus project with a set of services to interact with the project.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="options"/>, the <paramref name="configuration"/>, or the <paramref name="outputWriter"/> is <c>null</c>.
        /// </exception>
        public static AzureFunctionsServiceBusProject CreateNew(
            ServiceBusEntityType entityType, 
            AzureFunctionsServiceBusProjectOptions options, 
            TestConfig configuration, 
            ITestOutputHelper outputWriter)
        {
            Guard.NotNull(options, nameof(options), "Requires a set of project options to pass along to the project creation command");
            Guard.NotNull(configuration, nameof(configuration), "Requires a configuration instance to retrieve the configuration values to pass along to the to-be-created project");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires a test logger to write diagnostic information during the creation process");

            var project = new AzureFunctionsServiceBusProject(entityType, configuration, options, outputWriter);
            project.CreateNewProject(options);
            project.AddOrderMessageHandlerImplementation();
            project.AddLocalSettings();

            return project;
        }

        private void AddOrderMessageHandlerImplementation()
        {
            AddTypeAsFile<Order>();
            AddTypeAsFile<Customer>();
            AddTypeAsFile<OrderCreatedEventData>();

            AddTypeAsFile<WriteToFileMessageHandler>();
            UpdateFileInProject(RuntimeFileName, contents =>
                RemovesUserErrorsFromContents(contents)
                    .Replace("OrdersAzureServiceBusMessageHandler", nameof(WriteToFileMessageHandler))); 
        }

        private async Task StartAsync(ServiceBusEntityType entityType)
        {
            try
            {
                string serviceBusConnectionString = Configuration.GetServiceBusConnectionString(entityType);
                var properties = ServiceBusConnectionStringProperties.Parse(serviceBusConnectionString);
                string namespaceConnectionString = $"Endpoint={properties.Endpoint};SharedAccessKeyName={properties.SharedAccessKeyName};SharedAccessKey={properties.SharedAccessKey}";
                Environment.SetEnvironmentVariable("ServiceBusConnectionString", namespaceConnectionString);

                if (entityType is ServiceBusEntityType.Topic)
                {
                    await AddServiceBusTopicSubscriptionAsync(properties.EntityPath, namespaceConnectionString);
                }

                string instrumentationKey = Configuration.GetApplicationInsightsInstrumentationKey();
                Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", instrumentationKey);
                Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING", $"InstrumentationKey={instrumentationKey}");

                Run(Configuration.BuildConfiguration, TargetFramework.Net8_0);
                await WaitUntilTriggerIsAvailableAsync(Admin.Endpoint);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private static async Task AddServiceBusTopicSubscriptionAsync(string topic, string connectionString)
        {
            var client = new ServiceBusAdministrationClient(connectionString);
            var subscriptionName = "order-subscription";

            Response<bool> subscriptionExists = await client.SubscriptionExistsAsync(topic, subscriptionName);
            if (!subscriptionExists.Value)
            {
                await client.CreateSubscriptionAsync(topic, subscriptionName);
            }
        }

        /// <summary>
        /// Performs additional application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">The flag indicating whether or not the additional tasks should be disposed.</param>
        protected override void Disposing(bool disposing)
        {
            base.Disposing(disposing);
            Environment.SetEnvironmentVariable("ServiceBusConnectionString", null);
            Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", null);
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING", null);
        }
    }
}
