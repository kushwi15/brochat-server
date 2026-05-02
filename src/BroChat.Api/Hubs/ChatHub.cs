using BroChat.Application.Interfaces;
using BroChat.Domain.Entities;
using BroChat.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace BroChat.Api.Hubs;

public class ChatHub : Hub
{
    private readonly IAiService _aiService;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IGuestUsageRepository _guestUsageRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Tracks active streaming cancellation tokens per SignalR connection
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> _activeSessions = new();

    public ChatHub(
        IAiService aiService,
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IGuestUsageRepository guestUsageRepository,
        IUnitOfWork unitOfWork)
    {
        _aiService = aiService;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _guestUsageRepository = guestUsageRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>Client can invoke this to cancel the current streaming response.</summary>
    public Task CancelGeneration()
    {
        if (_activeSessions.TryRemove(Context.ConnectionId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (_activeSessions.TryRemove(Context.ConnectionId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
        return base.OnDisconnectedAsync(exception);
    }

    [Authorize]
    public async Task SendMessage(string conversationIdStr, string messageContent)
    {
        if (!Guid.TryParse(conversationIdStr, out var conversationId))
        {
            await Clients.Caller.SendAsync("Error", "Invalid conversation ID");
            return;
        }

        var userId = Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        
        if (conversation == null || conversation.UserId != userId)
        {
            await Clients.Caller.SendAsync("Error", "Conversation not found or access denied.");
            return;
        }

        // Register a CancellationTokenSource for this stream
        var cts = new CancellationTokenSource();
        _activeSessions[Context.ConnectionId] = cts;

        // Save User Message
        var userMessage = new Message
        {
            ConversationId = conversationId,
            Role = MessageRole.User,
            Content = messageContent
        };
        await _messageRepository.AddAsync(userMessage);
        await _unitOfWork.SaveChangesAsync();

        // Get history for AI Context
        var history = await _messageRepository.GetByConversationIdAsync(conversationId);

        // Stream AI response
        var fullAiResponse = "";
        var aiMessageId = Guid.NewGuid().ToString();

        try
        {
            await foreach (var chunk in _aiService.StreamChatResponseAsync(history.Where(m => m.Id != userMessage.Id), messageContent, cts.Token))
            {
                if (cts.Token.IsCancellationRequested) break;
                fullAiResponse += chunk;
                await Clients.Caller.SendAsync("ReceiveMessageChunk", aiMessageId, chunk, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // User cancelled — save whatever was streamed so far
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", "AI Error: " + ex.Message);
            return;
        }
        finally
        {
            _activeSessions.TryRemove(Context.ConnectionId, out _);
            cts.Dispose();
        }

        // Save AI Message
        var aiMessage = new Message
        {
            ConversationId = conversationId,
            Role = MessageRole.AI,
            Content = fullAiResponse
        };
        await _messageRepository.AddAsync(aiMessage);
        await _unitOfWork.SaveChangesAsync();

        await Clients.Caller.SendAsync("MessageComplete", aiMessageId);
    }

    public async Task SendGuestMessage(string guestId, string messageContent)
    {
        if (string.IsNullOrWhiteSpace(guestId))
        {
            await Clients.Caller.SendAsync("Error", "Invalid guest ID");
            return;
        }

        var usage = await _guestUsageRepository.GetByGuestIdAsync(guestId);
        if (usage == null)
        {
            usage = new GuestUsage { GuestId = guestId, RequestCount = 0, LastResetTime = DateTime.UtcNow };
        }

        // Reset count if 10 mins passed
        if (DateTime.UtcNow - usage.LastResetTime > TimeSpan.FromMinutes(10))
        {
            usage.RequestCount = 0;
            usage.LastResetTime = DateTime.UtcNow;
        }

        if (usage.RequestCount >= 5)
        {
            await Clients.Caller.SendAsync("Error", "Guest limit reached. Please register or wait 10 minutes.");
            return;
        }

        usage.RequestCount++;
        await _guestUsageRepository.UpsertAsync(usage);

        var aiMessageId = Guid.NewGuid().ToString();
        try
        {
            // Guests don't have history in this implementation for simplicity
            await foreach (var chunk in _aiService.StreamChatResponseAsync(Enumerable.Empty<Message>(), messageContent))
            {
                await Clients.Caller.SendAsync("ReceiveMessageChunk", aiMessageId, chunk);
            }
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", "AI Error: " + ex.Message);
            return;
        }

        await Clients.Caller.SendAsync("MessageComplete", aiMessageId);
    }
}
