namespace BroChat.Application.Interfaces;

public interface IAiService
{
    IAsyncEnumerable<string> StreamChatResponseAsync(IEnumerable<BroChat.Domain.Entities.Message> history, string newPrompt, CancellationToken cancellationToken = default);
}
