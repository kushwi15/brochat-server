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

        // MongoDB Atlas has a 38-character limit for database names.
        // We need to keep this short while still being unique per user.
        // Format: u_{first_8_chars_of_guid}
        var shortId = tenantId.Replace("-", "").Substring(0, 8);
        return $"u_{shortId}";
    }

}
