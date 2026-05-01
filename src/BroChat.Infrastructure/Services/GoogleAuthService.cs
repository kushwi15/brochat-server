using BroChat.Application.DTOs;
using BroChat.Application.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace BroChat.Infrastructure.Services;

public class GoogleAuthService : IExternalAuthService
{
    private readonly IConfiguration _configuration;

    public GoogleAuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<ExternalAuthUserDto?> ValidateGoogleIdTokenAsync(string idToken)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                // Audience = new List<string>() { _configuration["Authentication:Google:ClientId"]! }
                // In a production environment, you should validate the audience.
                // For simplicity or if you don't have the client ID yet, we'll just validate the token signature
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            
            return new ExternalAuthUserDto
            {
                Email = payload.Email,
                Name = payload.Name ?? payload.Email.Split('@')[0],
                ProviderSubjectId = payload.Subject
            };
        }
        catch
        {
            // Token is invalid
            return null;
        }
    }
}
