using System.IdentityModel.Tokens.Jwt;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using AssignmentManagement.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using Xunit;

namespace AssignmentManagement.Tests.Common;

public class JwtServiceTests
{
    [Fact]
    public void GenerateToken_ContainsExpectedClaims()
    {
        var options = Options.Create(new JwtOptions
        {
            Secret = "unit_test_secret_key_that_is_long_enough_1234567890",
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpiryMinutes = 60
        });
        var service = new JwtService(options);
        var user = new User { Id = 42, Email = "teacher@example.com", Role = UserRole.Teacher };

        var token = service.GenerateToken(user, out var expiresAt);
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.True(expiresAt > DateTime.UtcNow);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("test-issuer", parsed.Issuer);
        Assert.Contains(parsed.Claims, c => c.Value == "42");
        Assert.Contains(parsed.Claims, c => c.Value == "teacher@example.com");
        Assert.Contains(parsed.Claims, c => c.Value == "Teacher");
    }
}
