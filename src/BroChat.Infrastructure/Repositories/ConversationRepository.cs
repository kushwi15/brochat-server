using BroChat.Application.Interfaces;
using BroChat.Domain.Entities;
using BroChat.Infrastructure.Data;
using MongoDB.Driver;

namespace BroChat.Infrastructure.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly MongoDbContext _context;

    public ConversationRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Conversation?> GetByIdAsync(Guid id)
    {
        return await _context.Conversations.Find(c => c.Id == id).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Conversation>> GetAllByUserIdAsync(Guid userId)
    {
        return await _context.Conversations
            .Find(c => c.UserId == userId && c.IsActive)
            .SortByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Conversation conversation)
    {
        await _context.Conversations.InsertOneAsync(conversation);
    }

    public async Task UpdateAsync(Conversation conversation)
    {
        await _context.Conversations.ReplaceOneAsync(c => c.Id == conversation.Id, conversation);
    }

    public async Task DeleteAsync(Conversation conversation)
    {
        await _context.Conversations.DeleteOneAsync(c => c.Id == conversation.Id);
    }
}
