using System;

namespace Roster.Application.Ports.Security;

public interface IParticipantTokenService
{
    (string Token, string Hash) GenerateToken(Guid participantId, Guid gameId, string nickname);
    bool ValidateToken(string token, out Guid participantId, out Guid gameId);
}
