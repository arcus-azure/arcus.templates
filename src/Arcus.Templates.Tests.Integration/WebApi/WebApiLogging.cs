namespace Arcus.Templates.Tests.Integration.WebApi 
{
    /// <summary>
    /// Represents the available logging options for the web API project template.
    /// </summary>
    public enum WebApiLogging
    {
        /// <summary>
        /// Use the console as only logging provider.
        /// </summary>
        Default, 
        
        /// <summary>
        /// Adds Serilog with request logging and write to the console in structured entries.
        /// </summary>
        Serilog
    }
}