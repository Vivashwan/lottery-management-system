using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using ex1.services;

namespace ex1.Tests.ServicesTests;

public class TokenServiceTests
{
    [Fact]
    public void GenerateToken_ReturnsJwt_WithRoleClaim()
    {
        var dict = new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = "THIS_IS_A_TEST_SECRET_KEY_123456789012345",
            ["JwtSettings:Issuer"] = "test-issuer",
            ["JwtSettings:Audience"] = "test-audience",
            ["JwtSettings:ExpiryMinutes"] = "60"
        };

        var config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
        var logger = new Mock<ILogger<TokenService>>();

        var service = new TokenService(config, logger.Object);

        var token = service.GenerateToken(1, "a@a.com", "A", "B", "Manager");

        token.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type.EndsWith("/role") && c.Value == "Manager");
    }
}
