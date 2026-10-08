using System.Threading;
using System.Threading.Tasks;
using Roster.Application.Ports.Data;

namespace Roster.Infrastructure.Data.Repositories;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly RosterDbContext _context;

    public UnitOfWork(RosterDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
