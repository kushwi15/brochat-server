using BroChat.Application.Interfaces;
using BroChat.Infrastructure.Data;

namespace BroChat.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    public UnitOfWork(MongoDbContext context)
    {
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // MongoDB writes are atomic by default for single documents.
        // For multi-document transactions, we would use sessions.
        // To keep the existing flow, we return 1 (success).
        return Task.FromResult(1);
    }
}
