using System.Net.Http;
using System.Threading.Tasks;
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
        public Task<IActionResult> GetAuthorizedRoute(HttpRequestMessage request)
        {
            return Task.FromResult<IActionResult>(Ok());
        }

        [HttpGet]
        [Route(NoneAuthorizedRoute)]
        public Task<IActionResult> GetNoneAuthorizedRoute(HttpRequestMessage request)
        {
            return Task.FromResult<IActionResult>(Ok());
        }
    }
}