using BroChat.Application.Interfaces;
using BroChat.Domain.Entities;
using BroChat.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BroChat.Infrastructure.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly BroChatDbContext _context;

    public ConversationRepository(BroChatDbContext context)
    {
        _context = context;
    }

    public Task<Conversation?> GetByIdAsync(Guid id)
    {
        return _context.Conversations.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Conversation>> GetAllByUserIdAsync(Guid userId)
    {
        return await _context.Conversations
            .Where(c => c.UserId == userId && c.IsActive)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Conversation conversation)
    {
        await _context.Conversations.AddAsync(conversation);
    }

    public Task UpdateAsync(Conversation conversation)
    {
        _context.Conversations.Update(conversation);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Conversation conversation)
    {
        _context.Conversations.Remove(conversation);
        return Task.CompletedTask;
    }
}
