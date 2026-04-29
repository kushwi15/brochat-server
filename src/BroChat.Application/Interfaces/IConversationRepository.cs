using BroChat.Domain.Entities;

namespace BroChat.Application.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id);
    Task<IEnumerable<Conversation>> GetAllByUserIdAsync(Guid userId);
    Task AddAsync(Conversation conversation);
    Task UpdateAsync(Conversation conversation);
    Task DeleteAsync(Conversation conversation);
}
