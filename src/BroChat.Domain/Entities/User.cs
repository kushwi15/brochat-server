namespace BroChat.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AuthProvider> AuthProviders { get; set; } = new List<AuthProvider>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
