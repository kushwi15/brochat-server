using BroChat.Application.Interfaces;
using BroChat.Domain.Entities;
using BroChat.Infrastructure.Data;
using MongoDB.Driver;

namespace BroChat.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly MongoDbContext _context;

    public UserRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.Find(u => u.Email == email).FirstOrDefaultAsync();
    }

    public async Task<User?> GetByProviderAsync(string providerName, string providerSubjectId)
    {
        return await _context.Users.Find(u => u.AuthProviders.Any(ap => ap.ProviderName == providerName && ap.ProviderSubjectId == providerSubjectId)).FirstOrDefaultAsync();
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.InsertOneAsync(user);
    }

    public async Task UpdateAsync(User user)
    {
        await _context.Users.ReplaceOneAsync(u => u.Id == user.Id, user);
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        var rt = await _context.RefreshTokens.Find(rt => rt.Token == token).FirstOrDefaultAsync();
        if (rt == null) return null;

        rt.User = await GetByIdAsync(rt.UserId);
        return rt;
    }

    public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
    {
        await _context.RefreshTokens.InsertOneAsync(refreshToken);
    }

    public async Task UpdateRefreshTokenAsync(RefreshToken refreshToken)
    {
        await _context.RefreshTokens.ReplaceOneAsync(rt => rt.Id == refreshToken.Id, refreshToken);
    }
}
