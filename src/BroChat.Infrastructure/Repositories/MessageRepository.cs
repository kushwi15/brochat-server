using BroChat.Application.Interfaces;
using BroChat.Domain.Entities;
using BroChat.Infrastructure.Data;
using MongoDB.Driver;

namespace BroChat.Infrastructure.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly MongoDbContext _context;

    public MessageRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Message>> GetByConversationIdAsync(Guid conversationId)
    {
        return await _context.Messages
            .Find(m => m.ConversationId == conversationId)
            .SortBy(m => m.Timestamp)
            .ToListAsync();
    }

    public async Task AddAsync(Message message)
    {
        await _context.Messages.InsertOneAsync(message);
    }
    
    public async Task DeleteByConversationIdAsync(Guid conversationId)
    {
        await _context.Messages.DeleteManyAsync(m => m.ConversationId == conversationId);
    }
}
