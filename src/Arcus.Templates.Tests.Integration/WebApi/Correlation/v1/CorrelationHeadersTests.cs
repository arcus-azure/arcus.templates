using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Bogus;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.WebApi.Correlation.v1
{
    public class CorrelationHeadersTests
    {
        private const string OperationHeaderName = "X-Operation-ID",
                             TransactionHeaderName = "X-Transaction-ID";

        private readonly ITestOutputHelper _outputWriter;

        private static readonly Faker BogusGenerator = new Faker();

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationHeadersTests"/> class.
        /// </summary>
        public CorrelationHeadersTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task GetHealth_WithoutCorrelationProjectOption_ReturnsOkWithoutCorrelationHeaders()
        {
            // Arrange
            var optionsWithCorrelation =
                new WebApiProjectOptions().WithExcludeCorrelation();

            using (var project = await WebApiProject.StartNewAsync(optionsWithCorrelation, _outputWriter))
            // Act
            using (HttpResponseMessage response = await project.Health.GetAsync())
            {
                // Assert
                Assert.NotNull(response);
                Assert.DoesNotContain(response.Headers, h => h.Key == OperationHeaderName);
                Assert.DoesNotContain(response.Headers, h => h.Key == TransactionHeaderName);
            }
        }

        [Fact]
        public async Task GetHealth_OutOfTheBox_ReturnsOkWithCorrelationHeaders()
        {
            // Arrange
            using (var project = await WebApiProject.StartNewAsync(_outputWriter))
            // Act
            using (HttpResponseMessage response = await project.Health.GetAsync())
            {
                // Assert
                Assert.NotNull(response);
                AssertNonBlankResponseHeader(response, OperationHeaderName);
                AssertNonBlankResponseHeader(response, TransactionHeaderName);
            }
        }

        [Fact]
        public async Task GetHealth_OutOfTheBox_AndTransactionIdRequestHeader_ReturnsOkWithCorrelationHeadersAndSameTransactionId()
        {
            // Arrange
            string transactionId = BogusGenerator.Random.Hexadecimal(32, prefix: null);
            string operationParentId = BogusGenerator.Random.Hexadecimal(16, prefix: null);
            using (var project = await WebApiProject.StartNewAsync(_outputWriter))
            {
                // Act
                using (HttpResponseMessage response = 
                    await project.Health.GetAsync(
                        request => request.Headers.Add("traceparent", $"00-{transactionId}-{operationParentId}-00")))
                {
                    // Assert
                    AssertNonBlankResponseHeader(response, OperationHeaderName);
                    string actualTransactionId = AssertNonBlankResponseHeader(response, TransactionHeaderName);
                    Assert.Equal(transactionId, actualTransactionId);
                }
            }
        }

        private static string AssertNonBlankResponseHeader(HttpResponseMessage response, string headerName)
        {
            (string headerKey, IEnumerable<string> headerValues) = 
                Assert.Single(response.Headers, h => h.Key == headerName);

            Assert.NotNull(headerValues);
            string header = Assert.Single(headerValues);
            Assert.False(
                String.IsNullOrWhiteSpace(header), 
                $"Response header '{headerName}' doesn't have a non-blank value");

            return header;
        }
    }
}
