using System;
using System.Threading;
using System.Threading.Tasks;
using Roster.Application.Ports.Data;

namespace Roster.Application.Services;

/// <summary>Edits the settings of a game while nobody has joined it yet.</summary>
public sealed class GameSettingsService(
    IGameRepository gameRepository,
    IParticipantRepository participantRepository,
    IUnitOfWork unitOfWork)
{
    /// <summary>The lineup size can be changed only until the first participant joins.</summary>
    public async Task<bool> CanEditAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var participants = await participantRepository.GetParticipantsByGameIdAsync(gameId, cancellationToken);
        return participants.Count == 0;
    }

    /// <summary>
    /// The name can always be changed; the lineup size only until the first participant joins.
    /// </summary>
    public async Task UpdateAsync(Guid gameId, string name, int lineupSize, CancellationToken cancellationToken = default)
    {
        var game = await gameRepository.GetByIdAsync(gameId, cancellationToken)
            ?? throw new ArgumentException($"Game {gameId} not found.");

        if (lineupSize != game.LineupSize)
        {
            if (!await CanEditAsync(gameId, cancellationToken))
                throw new InvalidOperationException("The lineup size cannot be changed once a participant has joined.");

            game.ChangeLineupSize(lineupSize);
        }

        game.Rename(name);
        gameRepository.Update(game);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
