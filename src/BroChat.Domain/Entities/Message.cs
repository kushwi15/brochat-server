using BroChat.Domain.Enums;

namespace BroChat.Domain.Entities;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public List<FileAttachment> Attachments { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public Conversation? Conversation { get; set; }
}

public class FileAttachment
{
    public string Url { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
