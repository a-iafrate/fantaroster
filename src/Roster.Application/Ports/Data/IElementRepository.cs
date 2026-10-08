using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roster.Domain.Elements;

namespace Roster.Application.Ports.Data;

/// <summary>
/// Provides data access operations for Element entities.
/// </summary>
public interface IElementRepository
{
    Task<Element?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Element>> GetByGameIdAsync(Guid gameId, CancellationToken cancellationToken = default);
    void Add(Element element);
    void Update(Element element);
}
