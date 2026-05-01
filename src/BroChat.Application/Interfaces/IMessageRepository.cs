using BroChat.Domain.Entities;

namespace BroChat.Application.Interfaces;

public interface IMessageRepository
{
    Task<IEnumerable<Message>> GetByConversationIdAsync(Guid conversationId);
    Task AddAsync(Message message);
    Task DeleteByConversationIdAsync(Guid conversationId);
}
