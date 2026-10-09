using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Roster.Application.Ports.Data;
using Roster.Domain.Referees;

namespace Roster.Application.Services;

public sealed class RefereeManagementService
{
    private readonly IRefereeRepository _refereeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RefereeManagementService(IRefereeRepository refereeRepository, IUnitOfWork unitOfWork)
    {
        _refereeRepository = refereeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<Referee>> GetRefereesAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        return await _refereeRepository.GetByGameIdAsync(gameId, cancellationToken);
    }

    public async Task<string> CreateRefereeAsync(Guid gameId, string displayName, CancellationToken cancellationToken = default)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        var referee = new Referee(Guid.NewGuid(), gameId, displayName, hash, Array.Empty<string>());
        _refereeRepository.Add(referee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return token;
    }

    public async Task DeleteRefereeAsync(Guid refereeId, CancellationToken cancellationToken = default)
    {
        var referee = await _refereeRepository.GetByIdAsync(refereeId, cancellationToken);
        if (referee != null)
        {
            _refereeRepository.Delete(referee);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
