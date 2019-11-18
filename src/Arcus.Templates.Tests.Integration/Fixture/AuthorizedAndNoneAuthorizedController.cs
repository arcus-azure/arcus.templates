using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.Templates.Tests.Integration.Fixture
{
    [ApiController]
    public class AuthorizedAndNoneAuthorizedController : ControllerBase
    {
        public const string AuthorizedRoute = "auth/authorized",
                            NoneAuthorizedRoute = "auth/none";

        [HttpGet]
        [Route(AuthorizedRoute)]
        [Authorize]
        public IActionResult GetAuthorizedRoute(HttpRequestMessage request)
        {
            return Ok();
        }

        [HttpGet]
        [Route(NoneAuthorizedRoute)]
        public IActionResult GetNoneAuthorizedRoute(HttpRequestMessage request)
        {
            return Ok();
        }
    }
}