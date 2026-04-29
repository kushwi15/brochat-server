namespace BroChat.Domain.Entities;

public class AuthProvider
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string ProviderName { get; set; } = string.Empty; // e.g. "Google"
    public string ProviderSubjectId { get; set; } = string.Empty; // e.g. Google user ID

    public User? User { get; set; }
}
