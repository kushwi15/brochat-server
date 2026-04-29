using BroChat.Infrastructure.Services;
using BroChat.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Moq;
using System.IdentityModel.Tokens.Jwt;

namespace BroChat.Tests.Services;

public class JwtTokenServiceTests
{
    [Fact]
    public void GenerateJwtToken_ShouldReturnValidToken()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["Jwt:Secret"]).Returns("SUPER_SECRET_KEY_REPLACE_ME_IN_PRODUCTION_NEEDS_TO_BE_AT_LEAST_32_CHARS_LONG");
        mockConfig.Setup(c => c["Jwt:ExpiryMinutes"]).Returns("15");
        mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        mockConfig.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

        var service = new JwtTokenService(mockConfig.Object);
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.com" };

        // Act
        var token = service.GenerateJwtToken(user);

        // Assert
        Assert.False(string.IsNullOrEmpty(token));
        
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        Assert.Equal("TestIssuer", jwtToken.Issuer);
        Assert.Equal("TestAudience", jwtToken.Audiences.First());
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnValidRefreshToken()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["Jwt:RefreshTokenExpiryDays"]).Returns("7");

        var service = new JwtTokenService(mockConfig.Object);
        var userId = Guid.NewGuid();

        // Act
        var refreshToken = service.GenerateRefreshToken(userId);

        // Assert
        Assert.NotNull(refreshToken);
        Assert.Equal(userId, refreshToken.UserId);
        Assert.False(string.IsNullOrEmpty(refreshToken.Token));
        Assert.True(refreshToken.ExpiresAt > DateTime.UtcNow.AddDays(6));
    }
}
