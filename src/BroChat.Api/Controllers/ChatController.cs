using System.Security.Claims;
using BroChat.Application.DTOs;
using BroChat.Application.Interfaces;
using BroChat.Domain.Entities;
using BroChat.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BroChat.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChatController(IConversationRepository conversationRepository, IMessageRepository messageRepository, IUnitOfWork unitOfWork)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _unitOfWork = unitOfWork;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var conversations = await _conversationRepository.GetAllByUserIdAsync(GetUserId());
        return Ok(conversations.Select(c => new ConversationDto
        {
            Id = c.Id,
            Title = c.Title,
            CreatedAt = c.CreatedAt,
            IsActive = c.IsActive
        }));
    }

    [HttpPost("conversation")]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
    {
        var conversation = new Conversation
        {
            UserId = GetUserId(),
            Title = request.Title
        };

        await _conversationRepository.AddAsync(conversation);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new ConversationDto
        {
            Id = conversation.Id,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            IsActive = conversation.IsActive
        });
    }

    [HttpGet("{conversationId}/messages")]
    public async Task<IActionResult> GetMessages(Guid conversationId)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        if (conversation == null || conversation.UserId != GetUserId())
        {
            return NotFound();
        }

        var messages = await _messageRepository.GetByConversationIdAsync(conversationId);
        return Ok(messages.Select(m => new MessageDto
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            Role = m.Role,
            Content = m.Content,
            Timestamp = m.Timestamp
        }));
    }

    [HttpDelete("{conversationId}")]
    public async Task<IActionResult> DeleteConversation(Guid conversationId)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        if (conversation == null || conversation.UserId != GetUserId())
        {
            return NotFound();
        }

        await _messageRepository.DeleteByConversationIdAsync(conversationId);
        await _conversationRepository.DeleteAsync(conversation);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{conversationId}")]
    public async Task<IActionResult> UpdateConversation(Guid conversationId, [FromBody] UpdateConversationRequest request)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        if (conversation == null || conversation.UserId != GetUserId())
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            conversation.Title = request.Title;
            await _conversationRepository.UpdateAsync(conversation);
            await _unitOfWork.SaveChangesAsync();
        }

        return Ok(new ConversationDto
        {
            Id = conversation.Id,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            IsActive = conversation.IsActive
        });
    }

    [HttpPatch("message/{messageId}")]
    public async Task<IActionResult> UpdateMessage(Guid messageId, [FromBody] UpdateMessageRequest request)
    {
        var message = await _messageRepository.GetByIdAsync(messageId);
        if (message == null)
        {
            return NotFound();
        }

        // Verify ownership via conversation
        var conversation = await _conversationRepository.GetByIdAsync(message.ConversationId);
        if (conversation == null || conversation.UserId != GetUserId())
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(request.Content))
        {
            message.Content = request.Content;
            await _messageRepository.UpdateAsync(message);
            await _unitOfWork.SaveChangesAsync();
        }

        return Ok(new MessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            Role = message.Role,
            Content = message.Content,
            Timestamp = message.Timestamp
        });
    }
}
