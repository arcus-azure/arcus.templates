using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker.Health
{
    /// <summary>
    /// Service to interact with the exposed health report information of worker projects.
    /// </summary>
    public class HealthEndpointService
    {
        private readonly int _healthPort;
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthEndpointService"/> class.
        /// </summary>
        /// <param name="healthPort">The local TCP port on which the health report information is exposed by the worker.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        public HealthEndpointService(int healthPort, ITestOutputHelper outputWriter)
        {
            Guard.NotLessThanOrEqualTo<int, ArgumentException>(
                healthPort, 
                threshold: 0, 
                message: "Local TCP port on which the health report is exposed by the worker should be greater than zero");
            Guard.NotNull(outputWriter, nameof(outputWriter));

            _healthPort = healthPort;
            _outputWriter = outputWriter;
        }

        /// <summary>
        /// Probe for a health report at a worker's pre-configured local health port.
        /// </summary>
        public async Task<HealthStatus> ProbeHealthAsync()
        {
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(IPAddress.Parse("127.0.0.1"), _healthPort);
                _outputWriter.WriteLine("Connected to TCP health port {0}", _healthPort);

                using (NetworkStream clientStream = client.GetStream())
                using (var reader = new StreamReader(clientStream))
                {
                    _outputWriter.WriteLine("Probe for health report at TCP port {0}", _healthPort);
                    string healthReportJson = await reader.ReadToEndAsync();

                    Assert.False(String.IsNullOrWhiteSpace(healthReportJson), $"Probed health at TCP port {_healthPort} report cannot be blank");
                    JObject json = JObject.Parse(healthReportJson);

                    var status = Enum.Parse<HealthStatus>(json["status"].ToString());
                    return status;
                }
            }
        }
    }
}
