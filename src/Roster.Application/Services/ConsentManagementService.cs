using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Roster.Application.Ports.Data;
using Roster.Domain.Elements;

namespace Roster.Application.Services;

public sealed class ConsentManagementService
{
    private readonly IElementRepository _elementRepository;
    private readonly IConsentInvitationRepository _invitationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ConsentManagementService(
        IElementRepository elementRepository,
        IConsentInvitationRepository invitationRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _elementRepository = elementRepository;
        _invitationRepository = invitationRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Generates a new consent invitation for an element.
    /// Returns the raw token (which must be shown to the organizer to share, as it's not stored in plain text).
    /// </summary>
    public async Task<string> GenerateInvitationAsync(Guid elementId, string? contact, CancellationToken cancellationToken = default)
    {
        var element = await _elementRepository.GetByIdAsync(elementId, cancellationToken);
        if (element == null)
            throw new ArgumentException("Element not found");

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").Replace("=", "");

        var tokenHash = HashToken(token);

        var invitation = new ConsentInvitation(
            Guid.NewGuid(),
            elementId,
            tokenHash,
            contact,
            _timeProvider.GetUtcNow());

        _invitationRepository.Add(invitation);

        element.UpdateConsent(ConsentStatus.Pending);
        _elementRepository.Update(element);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return token;
    }

    public async Task<ConsentInvitation?> GetInvitationByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(token);
        return await _invitationRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
    }

    public async Task AnswerInvitationAsync(string token, bool accepted, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(token);
        var invitation = await _invitationRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (invitation == null)
            throw new ArgumentException("Invalid or expired invitation");

        if (invitation.AnsweredAt.HasValue)
            throw new InvalidOperationException("Invitation already answered");

        var element = await _elementRepository.GetByIdAsync(invitation.ElementId, cancellationToken);
        if (element == null)
            throw new InvalidOperationException("Element no longer exists");

        invitation.MarkAnswered(_timeProvider.GetUtcNow());
        _invitationRepository.Update(invitation);

        element.UpdateConsent(accepted ? ConsentStatus.Accepted : ConsentStatus.Declined);
        _elementRepository.Update(element);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<ConsentInvitation>> GetInvitationsForElementAsync(Guid elementId, CancellationToken cancellationToken = default)
    {
        return await _invitationRepository.GetByElementIdAsync(elementId, cancellationToken);
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
