using BroChat.Domain.Entities;

namespace BroChat.Application.Interfaces;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(Guid id);
    Task<IEnumerable<Message>> GetByConversationIdAsync(Guid conversationId);
    Task AddAsync(Message message);
    Task UpdateAsync(Message message);
    Task DeleteAsync(Message message);
    Task DeleteByConversationIdAsync(Guid conversationId);
}
