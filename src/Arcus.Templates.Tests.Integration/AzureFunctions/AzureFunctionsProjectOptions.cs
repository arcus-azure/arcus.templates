using System;
using Arcus.Templates.Tests.Integration.Fixture;

namespace Arcus.Templates.Tests.Integration.AzureFunctions
{
    /// <summary>
    /// Represents an simple set of options used in the Azure Functions project templates.
    /// </summary>
    public class AzureFunctionsProjectOptions : ProjectOptions
    {
        /// <summary>
        /// Gets the Azure Functions worker type the project should target.
        /// </summary>
        public FunctionsWorker FunctionsWorker { get; protected set; } = FunctionsWorker.Isolated;

        /// <summary>
        /// Sets the Azure Functions worker type of the project template.
        /// </summary>
        /// <param name="workerType">The functions worker type.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="workerType"/> is outside the bounds of the enumeration.</exception>
        protected void SetFunctionsWorker(FunctionsWorker workerType)
        {
            string workerTyperArgument = DetermineFunctionsWorkerArgument(workerType);
            AddOption($"--functions-worker {workerTyperArgument}");

            FunctionsWorker = workerType;
        }

        private static string DetermineFunctionsWorkerArgument(FunctionsWorker workerType)
        {
            switch (workerType)
            {
                case FunctionsWorker.InProcess: return "inProcess";
                case FunctionsWorker.Isolated: return "isolated";
                default:
                    throw new ArgumentOutOfRangeException(nameof(workerType), workerType, "Unknown functions worker type");
            }
        }
    }
}
