using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Roster.Application.Ports.Security;

namespace Roster.Infrastructure.Security;

public sealed class DataProtectionParticipantTokenService : IParticipantTokenService
{
    private readonly IDataProtector _protector;

    public DataProtectionParticipantTokenService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("Roster.ParticipantToken");
    }

    public (string Token, string Hash) GenerateToken(Guid participantId, Guid gameId, string nickname)
    {
        var payload = JsonSerializer.Serialize(new { P = participantId, G = gameId, N = nickname });
        var token = _protector.Protect(payload);

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        var hash = Convert.ToBase64String(hashBytes);

        return (token, hash);
    }

    public bool ValidateToken(string token, out Guid participantId, out Guid gameId)
    {
        participantId = Guid.Empty;
        gameId = Guid.Empty;

        try
        {
            var payload = _protector.Unprotect(token);
            var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            if (root.TryGetProperty("P", out var pElement) && pElement.TryGetGuid(out var pId) &&
                root.TryGetProperty("G", out var gElement) && gElement.TryGetGuid(out var gId))
            {
                participantId = pId;
                gameId = gId;
                return true;
            }
        }
        catch
        {
            // Invalid token, tampered or expired
        }

        return false;
    }
}
