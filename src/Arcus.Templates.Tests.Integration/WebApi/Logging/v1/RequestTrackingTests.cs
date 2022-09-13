using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Microsoft.Azure.ApplicationInsights.Query;
using Microsoft.Azure.ApplicationInsights.Query.Models;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.WebApi.Logging.v1
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class RequestTrackingTests : ApplicationInsightsTests
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestTrackingTests" /> class.
        /// </summary>
        public RequestTrackingTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        [Fact]
        public async Task GetSabotagedEndpoint_TracksFailedResponse_ReturnsFailedResponse()
        {
            // Arrange
            var optionsWithSerilogLogging =
                new WebApiProjectOptions().WithSerilogLogging(ApplicationInsightsConfig.InstrumentationKey);
            
            using (var project = WebApiProject.CreateNew(Configuration, optionsWithSerilogLogging, Logger))
            {
                project.AddTypeAsFile<SaboteurController>();
                await project.StartAsync();

                project.TearDownOptions = TearDownOptions.KeepProjectDirectory;
                // Act
                using (HttpResponseMessage response = await project.Root.GetAsync(SaboteurController.Route))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                    await RetryAssertUntilTelemetryShouldBeAvailableAsync(async client =>
                    {
                        EventsResults<EventsRequestResult> results =
                            await client.Events.GetRequestEventsAsync(ApplicationInsightsConfig.ApplicationId, timespan: PastHalfHourTimeSpan);

                        Assert.Contains(results.Value, result =>
                        {
                            return result.Request.Url.Contains("sabotage") && result.Request.ResultCode == "500";
                        });
                    });
                }
            }
        }
    }
}
