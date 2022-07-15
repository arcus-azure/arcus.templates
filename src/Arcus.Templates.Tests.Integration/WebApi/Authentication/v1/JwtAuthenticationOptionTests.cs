using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
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
            string issuer = $"issuer-{Guid.NewGuid()}";
            string audience = $"audience-{Guid.NewGuid()}";
            var options = new WebApiProjectOptions().WithJwtAuthentication(key, issuer, audience);

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
            string issuer = $"issuer-{Guid.NewGuid()}";
            string audience = $"audience-{Guid.NewGuid()}";
            string jwtToken = CreateToken(key, issuer, audience);
            var jwtHeader = AuthenticationHeaderValue.Parse("Bearer " + jwtToken);

            var options = new WebApiProjectOptions().WithJwtAuthentication(key, issuer, audience);

            using (var project = await WebApiProject.StartNewAsync(options, _outputWriter))
            // Act
            using (HttpResponseMessage response = await project.Health.GetAsync(request => request.Headers.Authorization = jwtHeader))
            {
                // Assert
                Assert.NotNull(response);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async Task JwtAuthenticationOption_GetSwaggerDocs_ContainsJwtSecurityScheme()
        {
            // Arrange
            string key = $"secret-{Guid.NewGuid()}";
            string issuer = $"issuer-{Guid.NewGuid()}";
            string audience = $"audience-{Guid.NewGuid()}";
            string jwtToken = CreateToken(key, issuer, audience);
            var jwtHeader = AuthenticationHeaderValue.Parse("Bearer " + jwtToken);

            var options = new WebApiProjectOptions().WithJwtAuthentication(key, issuer, audience);

            using (var project = await WebApiProject.StartNewAsync(options, _outputWriter))
            {
                // Act
                using (HttpResponseMessage response = await project.Swagger.GetSwaggerDocsAsync())
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    var reader = new OpenApiStreamReader();
                    using (Stream json = await response.Content.ReadAsStreamAsync())
                    {
                        OpenApiDocument document = reader.Read(json, out OpenApiDiagnostic diagnostic);

                        Assert.NotNull(document.Components);
                        (string schemeName, OpenApiSecurityScheme componentScheme) = Assert.Single(document.Components.SecuritySchemes);
                        Assert.Equal("Bearer", schemeName);
                        Assert.Equal(SecuritySchemeType.Http, componentScheme.Type);
                        
                        OpenApiSecurityRequirement requirement = Assert.Single(document.SecurityRequirements);
                        Assert.NotNull(requirement);
                        (OpenApiSecurityScheme requirementScheme, IList<string> scopes) = Assert.Single(requirement);
                        Assert.Equal("Bearer", requirementScheme.Reference.Id);
                    }
                }
            }
        }

        private static string CreateToken(string key, string issuer, string audience)
        {
            var claims = new[] 
            {
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(JwtRegisteredClaimNames.Exp, $"{new DateTimeOffset(DateTime.Now.AddMinutes(1)).ToUnixTimeSeconds()}"),
                new Claim(JwtRegisteredClaimNames.Nbf, $"{new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds()}")        
            };
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
         
            var securityToken = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
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
