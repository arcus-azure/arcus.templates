﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.AzureFunctions.Admin;
using Arcus.Templates.Tests.Integration.AzureFunctions.Configuration;
using Arcus.Templates.Tests.Integration.AzureFunctions.Databricks.JobMetrics.Configuration;
using Arcus.Templates.Tests.Integration.AzureFunctions.Databricks.JobMetrics.MetricReporting;
using Arcus.Templates.Tests.Integration.AzureFunctions.Http.Configuration;
using Arcus.Templates.Tests.Integration.Fixture;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Http.Health
{
    [Collection(TestCollections.Docker)]
    [Trait("Category", TestTraits.Docker)]
    public class HttpHealthDockerTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpHealthDockerTests"/> class.
        /// </summary>
        public HttpHealthDockerTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }
        
        [Fact]
        public async Task AzureFunctionsHttpProject_WithoutOptions_ShouldAnswerToAdministratorEndpoint()
        {
            // Arrange
            var configuration = TestConfig.Create();
            AzureFunctionHttpConfig httpConfig = configuration.GetAzureFunctionHttpConfig();
            var service = new AdminEndpointService(httpConfig.HttpPort, AzureFunctionsHttpProject.OrderFunctionName, _outputWriter);

            // Act / Assert
            await service.TriggerFunctionAsync();
        }
    }
}
