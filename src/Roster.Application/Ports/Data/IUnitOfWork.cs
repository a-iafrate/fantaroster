using System.Threading;
using System.Threading.Tasks;

namespace Roster.Application.Ports.Data;

/// <summary>
/// Defines a contract for a unit of work that manages database transactions and changes.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
