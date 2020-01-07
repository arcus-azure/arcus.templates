using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.WebApi.Correlation
{
    public class CorrelationTests
    {
        private const string TransactionIdHeader = "X-Transaction-ID",
                             OperationIdHeader = "X-Operation-ID";

        private readonly ITestOutputHelper _outputWriter;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationTests"/> type.
        /// </summary>
        public CorrelationTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task SettingCorrelationProjectOption_SetsCorrelationHeaders()
        {
            // Arrange
            var options = new WebApiProjectOptions().WithCorrelation();
            using (var project = await WebApiProject.StartNewAsync(options, _outputWriter)) 
            // Act
            using (HttpResponseMessage response = await project.Health.GetAsync())
            {
                // Assert
                Assert.NotNull(response);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                (string transactionIdHeader, IEnumerable<string> transactionIds) =
                    Assert.Single(response.Headers, h => h.Key == TransactionIdHeader);

                (string operationIdHeader, IEnumerable<string> operationIds) =
                    Assert.Single(response.Headers, h => h.Key == OperationIdHeader);

                Assert.False(String.IsNullOrWhiteSpace(Assert.Single(transactionIds)));
                Assert.False(String.IsNullOrWhiteSpace(Assert.Single(operationIds)));
            }
        }

        [Fact]
        public async Task NotSettingCorrelationProjectOption_DoesntSetCorrelationHeaders()
        {
            // Arrange
            using (var project = await WebApiProject.StartNewAsync(_outputWriter))
            // Act
            using (HttpResponseMessage response = await project.Health.GetAsync())
            {
                // Assert
                Assert.NotNull(response);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.DoesNotContain(response.Headers, h => h.Key == TransactionIdHeader);
                Assert.DoesNotContain(response.Headers, h => h.Key == OperationIdHeader);
            }
        }
    }
}
