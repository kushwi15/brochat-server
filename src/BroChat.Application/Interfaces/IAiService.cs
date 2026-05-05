namespace BroChat.Application.Interfaces;

public interface IAiService
{
    IAsyncEnumerable<string> StreamChatResponseAsync(
        IEnumerable<BroChat.Domain.Entities.Message> history, 
        string newPrompt, 
        IEnumerable<BroChat.Domain.Entities.FileAttachment>? attachments = null,
        CancellationToken cancellationToken = default);
}
