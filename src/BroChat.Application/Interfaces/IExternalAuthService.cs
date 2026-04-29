using BroChat.Application.DTOs;

namespace BroChat.Application.Interfaces;

public interface IExternalAuthService
{
    Task<ExternalAuthUserDto?> ValidateGoogleIdTokenAsync(string idToken);
}
