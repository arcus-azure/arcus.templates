using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.AzureFunctions.Timer.Fixture;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Logging;
using Arcus.Templates.Tests.Integration.Worker.Configuration;
using Arcus.Templates.Tests.Integration.Worker.Fixture;
using Azure;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using GuardNet;
using Microsoft.Extensions.Azure;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Timer
{
    /// <summary>
    /// Project template to create new Azure Functions Timer projects.
    /// </summary>
    [DebuggerDisplay("Project = {ProjectDirectory.FullName}")]
    public class AzureFunctionsTimerProject : AzureFunctionsProject, IAsyncDisposable
    {
        private readonly string _eventSubject = Guid.NewGuid().ToString();
        private readonly TestServiceBusMessageEventConsumer _eventGrid;

        private AzureFunctionsTimerProject(
            TestConfig configuration, 
            AzureFunctionsProjectOptions options, 
            TestServiceBusMessageEventConsumer consumer,
            ITestOutputHelper outputWriter) 
            : base(configuration.GetAzureFunctionsTimerProjectDirectory(), 
                   configuration, 
                   options, 
                   outputWriter)
        {
            _eventGrid = consumer;
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

            var consumer = await TestServiceBusMessageEventConsumer.StartNewAsync(config, new XunitTestLogger(outputWriter));
            var project = new AzureFunctionsTimerProject(config, options, consumer, outputWriter);

            project.CreateNewProject(options);
            project.AddLocalSettings();
            project.UpdateFileInProject(project.RuntimeFileName, contents => project.RemovesUserErrorsFromContents(contents));
            project.AddEventGridPublishing();

            await project.StartAsync();

            return project;
        }

        private void AddEventGridPublishing()
        {
            AddPackage("Arcus.EventGrid.Core", "3.3.0");
            AddTypeAsFile<TimerTriggeredEvent>();

            var functionFileName = "TimerFunction.cs";
            UpdateFileWithUsingStatement(functionFileName, typeof(EventGridPublisherClient));
            UpdateFileWithUsingStatement(functionFileName, typeof(IAzureClientFactory<>));
            UpdateFileWithUsingStatement(functionFileName, typeof(AzureKeyCredential));
            UpdateFileWithUsingStatement(functionFileName, typeof(CloudEvent));

            UpdateFileInProject(functionFileName,
                contents => contents.Replace("// Execute timed action...", 
                    $"var eventGridTopic = _configuration.GetValue<string>(\"EVENTGRID_TOPIC_URI\");{Environment.NewLine}" +
                    $"var eventGridKey = _configuration.GetValue<string>(\"EVENTGRID_AUTH_KEY\");{Environment.NewLine}" +
                    $"var publisher = new {nameof(EventGridPublisherClient)}(new Uri(eventGridTopic), new {nameof(AzureKeyCredential)}(eventGridKey));{Environment.NewLine}" +
                    $"var eventBody = new {nameof(TimerTriggeredEvent)} {{ Id = Guid.NewGuid().ToString(), TriggeredDate = DateTimeOffset.UtcNow, TimerName = \"timer\", Subject = \"{_eventSubject}\" }};" +
                    "publisher.SendEvent(new CloudEvent(\"timer\", \"Timer.TimerTriggered\", eventBody));"));
        }

        /// <summary>
        /// Starts the Azure Functions Timer trigger project, created from the template.
        /// </summary>
        private async Task StartAsync()
        {
            try
            {
                EventGridConfig eventGridConfig = Configuration.GetEventGridConfig();
                Environment.SetEnvironmentVariable("EVENTGRID_TOPIC_URI", eventGridConfig.TopicUri);
                Environment.SetEnvironmentVariable("EVENTGRID_AUTH_KEY", eventGridConfig.AuthenticationKey);

                string instrumentationKey = Configuration.GetApplicationInsightsInstrumentationKey();
                Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", instrumentationKey);
                Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING", $"InstrumentationKey={instrumentationKey}");

                Run(Configuration.BuildConfiguration, TargetFramework.Net6_0);
                await WaitUntilTriggerIsAvailableAsync(RootEndpoint);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// Retrieve the time triggered event automatically published by this timer Azure Functions;
        /// successfully consuming such an event corresponds with a correctly working application.
        /// </summary>
        public CloudEvent ConsumeTriggeredEvent()
        {
            return _eventGrid.ConsumeEvent<TimerTriggeredEvent>(ev => ev.Subject == _eventSubject);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await _eventGrid.DisposeAsync();
            Dispose();
        }

        /// <summary>
        /// Performs additional application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">The flag indicating whether or not the additional tasks should be disposed.</param>
        protected override void Disposing(bool disposing)
        {
            base.Disposing(disposing);

            Environment.SetEnvironmentVariable("EVENTGRID_TOPIC_URI", null);
            Environment.SetEnvironmentVariable("EVENTGRID_AUTH_KEY", null);
            Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", null);
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING", null);
        }
    }
}
