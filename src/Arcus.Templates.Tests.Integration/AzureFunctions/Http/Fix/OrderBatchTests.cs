using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Templates.AzureFunctions.Http;
using Arcus.Templates.AzureFunctions.Http.Model;
using Arcus.Templates.Tests.Integration.Fixture;
using Bogus;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Http.Fix
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class OrderBatchTests
    {
        private readonly ITestOutputHelper _outputWriter;

        private static readonly Faker BogusGenerator = new Faker();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="OrderBatchTests" /> class.
        /// </summary>
        public OrderBatchTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }
        
        [Theory]
        [InlineData(FunctionsWorker.InProcess)]
        [InlineData(FunctionsWorker.Isolated)]
        public async Task OrderFunction_ReceivedBatchedOrders_ReturnsSuccess(FunctionsWorker workerType)
        {
            // Arrange
            var config = TestConfig.Create();
            var options = new AzureFunctionsHttpProjectOptions().WithFunctionWorker(workerType);
            using (var project = AzureFunctionsHttpProject.CreateNew(config, options, _outputWriter))
            {
                string methodName = DetermineMethodName(workerType);
                project.UpdateFileInProject($"{nameof(OrderFunction)}.cs", contents =>
                {
                    return contents.Replace($"{methodName}<Order>", $"{methodName}<Order[]>");
                });
                await project.StartAsync();

                IEnumerable<Order> orders = BogusGenerator.Make(3, CreateRandomOrder);
                string json = JsonConvert.SerializeObject(orders.ToArray());

                // Act
                using (HttpResponseMessage response = await project.Order.PostAsync(json))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        private static string DetermineMethodName(FunctionsWorker workerType)
        {
            switch (workerType)
            {
                case FunctionsWorker.InProcess: return "GetJsonBodyAsync";
                case FunctionsWorker.Isolated: return "ReadFromJsonAsync";
                default:
                    throw new ArgumentOutOfRangeException(nameof(workerType), workerType, "Unknown functions worker type");
            }
        }

        private static Order CreateRandomOrder()
        {
            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                ArticleNumber = BogusGenerator.Random.String(1, 100),
                Scheduled = BogusGenerator.Date.RecentOffset()
            };

            return order;
        }
    }
}
