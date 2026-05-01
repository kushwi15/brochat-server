using BroChat.Application.Interfaces;
using BroChat.Domain.Entities;
using BroChat.Infrastructure.Data;
using MongoDB.Driver;

namespace BroChat.Infrastructure.Repositories;

public class GuestUsageRepository : IGuestUsageRepository
{
    private readonly MongoDbContext _context;

    public GuestUsageRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<GuestUsage?> GetByGuestIdAsync(string guestId)
    {
        return await _context.GuestUsages.Find(gu => gu.GuestId == guestId).FirstOrDefaultAsync();
    }

    public async Task UpsertAsync(GuestUsage guestUsage)
    {
        var filter = Builders<GuestUsage>.Filter.Eq(gu => gu.GuestId, guestUsage.GuestId);
        await _context.GuestUsages.ReplaceOneAsync(filter, guestUsage, new ReplaceOptions { IsUpsert = true });
    }
}
