using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.AzureFunctions.Http
{
    /// <summary>
    /// Project template to create new Azure Functions HTTP projects.
    /// </summary>
    [DebuggerDisplay("Project = {ProjectDirectory.FullName}")]
    public class AzureFunctionsHttpProject : AzureFunctionsProject
    {
        private AzureFunctionsHttpProject(
            TestConfig configuration, 
            ITestOutputHelper outputWriter) 
            : base(configuration.GetAzureFunctionsHttpProjectDirectory(), 
                   configuration, 
                   outputWriter)
        {
        }

        /// <summary>
        /// Starts a newly created project from the Azure Functions HTTP project template.
        /// </summary>
        /// <param name="configuration">The configuration to control the hosting of the to-be-created project.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A Azure Functions HTTP project with a full set of endpoint services to interact with the Azure Function.
        /// </returns>
        public static AzureFunctionsHttpProject StartNew(TestConfig configuration, ITestOutputHelper outputWriter)
        {
            AzureFunctionsHttpProject project = CreateNew(configuration, outputWriter);
            project.Start();

            return project;
        }

        private static AzureFunctionsHttpProject CreateNew(TestConfig configuration, ITestOutputHelper outputWriter)
        {
            var project = new AzureFunctionsHttpProject(configuration, outputWriter);
            project.CreateNewProject(new ProjectOptions());
            project.AddStorageAccount();
            
            return project;
        }

        private void Start()
        {
            Run(BuildConfiguration.Debug, TargetFramework.NetCoreApp31);
        }
    }
}
