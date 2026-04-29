using BroChat.Application.Interfaces;
using BroChat.Infrastructure.Data;

namespace BroChat.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly BroChatDbContext _context;

    public UnitOfWork(BroChatDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
