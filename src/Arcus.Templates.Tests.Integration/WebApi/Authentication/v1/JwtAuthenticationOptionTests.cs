using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.WebApi.Authentication.v1
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class JwtAuthenticationOptionTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtAuthenticationOptionTests"/> class.
        /// </summary>
        public JwtAuthenticationOptionTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task GetHealthWithoutBearerToken_WithJwtAuthenticationOption_ReturnsUnauthorized()
        {
            // Arrange
            string key = $"secret-{Guid.NewGuid()}";
            var options = new WebApiProjectOptions().WithJwtAuthentication(key);

            using (var project = await WebApiProject.StartNewAsync(options, _outputWriter))
                // Act
            using (HttpResponseMessage response = await project.Health.GetAsync())
            {
                // Assert
                Assert.NotNull(response);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Fact]
        public async Task GetHealthWithBearerToken_WithJwtAuthenticationOption_ReturnsOk()
        {
            // Arrange
            string key = $"secret-{Guid.NewGuid()}";
            string jwtToken = CreateToken(key);
            var jwtHeader = AuthenticationHeaderValue.Parse("Bearer " + jwtToken);

            var options = new WebApiProjectOptions().WithJwtAuthentication(key);

            using (var project = await WebApiProject.StartNewAsync(options, _outputWriter))
            // Act
            using (HttpResponseMessage response = await project.Health.GetAsync(request => request.Headers.Authorization = jwtHeader))
            {
                // Assert
                Assert.NotNull(response);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        private static string CreateToken(string key)
        {
            var claims = new[] 
            {
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(JwtRegisteredClaimNames.Exp, $"{new DateTimeOffset(DateTime.Now.AddMinutes(1)).ToUnixTimeSeconds()}"),
                new Claim(JwtRegisteredClaimNames.Nbf, $"{new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds()}")        
            };
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
         
            var securityToken = new JwtSecurityToken(
                issuer: "entity that generates the token",
                audience: "client of the app",
                claims: claims,
                notBefore: DateTime.Now,
                expires: DateTime.Now.AddMinutes(1),
                signingCredentials: new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256)
            );

            var securityTokenHandler = new JwtSecurityTokenHandler();
            return securityTokenHandler.WriteToken(securityToken);
        }
    }
}
