using Microsoft.AspNetCore.Mvc;

namespace Arcus.Templates.Tests.Integration.WebApi.Bug.EnumParameter
{
    /// <summary>
    /// Controller to test enum input serialization.
    /// </summary>
    [ApiController]
    [Route("api/v1/" + GetRoute)]
    public class EnumController : ControllerBase
    {
        public const string GetRoute = "enum";

        [HttpGet]
        public IActionResult Get([FromBody] TestEnum value)
        {
            return Ok();
        }
    }
}
