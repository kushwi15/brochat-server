using BroChat.Domain.Entities;

namespace BroChat.Application.Interfaces;

public interface ITokenService
{
    string GenerateJwtToken(User user);
    RefreshToken GenerateRefreshToken(Guid userId);
}
