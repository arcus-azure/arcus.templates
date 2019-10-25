namespace Arcus.Templates.Tests.Integration
{
    /// <summary>
    /// Collects all available test collection names to categorize tests.
    /// </summary>
    public class TestCollections
    {
        /// <summary>
        /// Gets the collection name for integration tests.
        /// </summary>
        public const string Integration = nameof(Integration);

        /// <summary>
        /// Gets the collection name for tests on the Docker endpoint.
        /// </summary>
        public const string Docker = nameof(Docker);
    }
}
