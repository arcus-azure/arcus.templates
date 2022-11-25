using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.WebApi.Correlation.v1
{
    public class CorrelationHeadersTests
    {
        private const string OperationHeaderName = "X-Operation-ID",
                             TransactionHeaderName = "X-Transaction-ID";

        private readonly ITestOutputHelper _outputWriter;

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
            var expectedTransactionId = $"transaction-{Guid.NewGuid():N}";
            using (var project = await WebApiProject.StartNewAsync(_outputWriter))
            {
                // Act
                using (HttpResponseMessage response = 
                    await project.Health.GetAsync(
                        request => request.Headers.Add(TransactionHeaderName, expectedTransactionId)))
                {
                    // Assert
                    AssertNonBlankResponseHeader(response, OperationHeaderName);
                    string actualTransactionId = AssertNonBlankResponseHeader(response, TransactionHeaderName);
                    Assert.Equal(expectedTransactionId, actualTransactionId);
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
