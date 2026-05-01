namespace BroChat.Application.DTOs;

public class ExternalAuthUserDto
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ProviderSubjectId { get; set; } = string.Empty;
}
