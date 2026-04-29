using BroChat.Application.Interfaces;
using BroChat.Domain.Entities;
using BroChat.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BroChat.Infrastructure.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly BroChatDbContext _context;

    public MessageRepository(BroChatDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Message>> GetByConversationIdAsync(Guid conversationId)
    {
        return await _context.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
    }

    public async Task AddAsync(Message message)
    {
        await _context.Messages.AddAsync(message);
    }
}
