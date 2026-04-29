using BroChat.Application.Interfaces;
using BroChat.Domain.Entities;
using BroChat.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BroChat.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly BroChatDbContext _context;

    public UserRepository(BroChatDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByIdAsync(Guid id)
    {
        return _context.Users.FirstOrDefaultAsync(u => u.Id == id);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        return _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByProviderAsync(string providerName, string providerSubjectId)
    {
        var authProvider = await _context.AuthProviders
            .Include(ap => ap.User)
            .FirstOrDefaultAsync(ap => ap.ProviderName == providerName && ap.ProviderSubjectId == providerSubjectId);
        
        return authProvider?.User;
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        return _context.RefreshTokens.Include(rt => rt.User).FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
    {
        await _context.RefreshTokens.AddAsync(refreshToken);
    }

    public Task UpdateRefreshTokenAsync(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Update(refreshToken);
        return Task.CompletedTask;
    }
}
