using BroChat.Domain.Enums;

namespace BroChat.Application.DTOs;

public class ConversationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

public class MessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public List<FileAttachmentDto> Attachments { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

public class FileAttachmentDto
{
    public string Url { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class CreateConversationRequest
{
    public string Title { get; set; } = string.Empty;
}

public class UpdateConversationRequest
{
    public string Title { get; set; } = string.Empty;
}

public class UpdateMessageRequest
{
    public string Content { get; set; } = string.Empty;
}

public class SendMessageRequest
{
    public Guid ConversationId { get; set; }
    public string Content { get; set; } = string.Empty;
}
