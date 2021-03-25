using System;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.Templates.Tests.Integration.WebApi.Logging.v1
{
    /// <summary>
    /// Represents an API controller that sabotage the HTTP endpoint by throwing an exception.
    /// </summary>
    [Route("api/v1")]
    [ApiController]
    public class SaboteurController : ControllerBase
    {
        public const string Route = "sabotage";
        
        [HttpGet]
        [Route(Route)]
        public IActionResult Sabotage()
        {
            throw new Exception("Sabotage the HTTP endpoint by throwing an general exception");
        }
    }
}
