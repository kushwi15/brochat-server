namespace BroChat.Application.Interfaces;

public interface ITenantService
{
    string? GetTenantId();
    string GetDatabaseName();
}
