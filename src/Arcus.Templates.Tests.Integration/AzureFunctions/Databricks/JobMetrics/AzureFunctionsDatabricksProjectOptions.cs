namespace Arcus.Templates.Tests.Integration.AzureFunctions.Databricks.JobMetrics
{
    /// <summary>
    /// Represents the additional user project options to change the <see cref="AzureFunctionsDatabricksProject"/> project contents and functionality.
    /// </summary>
    public class AzureFunctionsDatabricksProjectOptions : AzureFunctionsProjectOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionsDatabricksProjectOptions" /> class.
        /// </summary>
        public AzureFunctionsDatabricksProjectOptions()
        {
            FunctionsWorker = FunctionsWorker.InProcess;
        }

        /// <summary>
        /// Adds a project option that excludes all the Serilog logging functionality from the resulting project.
        /// </summary>
        public AzureFunctionsDatabricksProjectOptions WithExcludeSerilog()
        {
            AddOption("--exclude-serilog");
            return this;
        }
    }
}
