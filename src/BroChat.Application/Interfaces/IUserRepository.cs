using BroChat.Domain.Entities;

namespace BroChat.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByProviderAsync(string providerName, string providerSubjectId);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task<User?> GetByResetTokenAsync(string token);
    
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task AddRefreshTokenAsync(RefreshToken refreshToken);
    Task UpdateRefreshTokenAsync(RefreshToken refreshToken);
}
