using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Arcus.Templates.Tests.Integration.Fixture
{
    /// <summary>
    /// Fixture controller to toggle the <see cref="Get"/> response based on a <see cref="FeatureToggle"/> configuration key.
    /// </summary>
    [ApiController]
    [Route("api/v1/" + Route)]
    public class FeatureToggledController : ControllerBase
    {
        private readonly bool _toggled;

        /// <summary>
        /// Gets the route on which the feature toggled controller listens.
        /// </summary>
        public const string Route = "featuretoggle";

        /// <summary>
        /// Gets the configuration key where the feature toggle value is configured.
        /// </summary>
        public const string FeatureToggle = "FeatureToggle";

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureToggledController"/> class.
        /// </summary>
        /// <param name="configuration">The configuration where the <see cref="FeatureToggle"/> is specified.</param>
        public FeatureToggledController(IConfiguration configuration)
        {
            Guard.NotNull(configuration, nameof(configuration));

            _toggled = configuration.GetValue<bool>(FeatureToggle);
        }

        /// <summary>
        ///     Get feature toggled response.
        /// </summary>
        /// <returns></returns>
        [HttpGet(Name = "FeatureToggle_Get")]
        public IActionResult Get()
        {
            if (_toggled)
            {
                return Ok();
            }
            else
            {
                return StatusCode(StatusCodes.Status406NotAcceptable);
            }
        }
    }
}