using BroChat.Application.Interfaces;
using BroChat.Domain.Entities;
using BroChat.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace BroChat.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IAiService _aiService;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChatHub(
        IAiService aiService,
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IUnitOfWork unitOfWork)
    {
        _aiService = aiService;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _unitOfWork = unitOfWork;
    }

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
        try
        {
            await foreach (var chunk in _aiService.StreamChatResponseAsync(history.Where(m => m.Id != userMessage.Id), messageContent))
            {
                fullAiResponse += chunk;
                await Clients.Caller.SendAsync("ReceiveMessageChunk", chunk);
            }
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", "AI Error: " + ex.Message);
            return;
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

        await Clients.Caller.SendAsync("MessageComplete");
    }
}
