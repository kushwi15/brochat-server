using BroChat.Domain.Entities;

namespace BroChat.Application.Interfaces;

public interface IGuestUsageRepository
{
    Task<GuestUsage?> GetByGuestIdAsync(string guestId);
    Task UpsertAsync(GuestUsage guestUsage);
}
