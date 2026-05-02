using System.Security.Claims;
using BroChat.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace BroChat.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _defaultDatabaseName;

    public TenantService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _defaultDatabaseName = configuration["MongoDb:DatabaseName"] ?? "brochat";
    }

    public string? GetTenantId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        // Try to get UserId from JWT claims
        var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            // Fallback: Check if it's a guest (e.g. from a custom header or another claim)
            // For now, we'll return null for guests which will use the default database
            return null;
        }

        return userId;
    }

    public string GetDatabaseName()
    {
        var tenantId = GetTenantId();
        
        if (string.IsNullOrEmpty(tenantId))
        {
            return _defaultDatabaseName;
        }

        // Return a database name specific to the user
        // We sanitize the ID to ensure it's a valid MongoDB database name (alphanumeric and underscores)
        var sanitizedId = tenantId.Replace("-", "_").ToLower();
        return $"{_defaultDatabaseName}_user_{sanitizedId}";
    }
}
