namespace BroChat.Domain.Entities;

public class GuestUsage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string GuestId { get; set; } = string.Empty;
    public int RequestCount { get; set; }
    public DateTime LastResetTime { get; set; } = DateTime.UtcNow;
}
