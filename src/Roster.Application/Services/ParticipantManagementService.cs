using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roster.Application.Participants.Commands;
using Roster.Application.Ports.Data;
using Roster.Application.Ports.Security;
using Roster.Domain.Games;
using Roster.Domain.Participants;

namespace Roster.Application.Services;

public sealed class ParticipantManagementService
{
    private readonly IGameRepository _gameRepository;
    private readonly IParticipantRepository _participantRepository;
    private readonly IElementRepository _elementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IParticipantTokenService _tokenService;
    private readonly IProfanityFilter _profanityFilter;
    private readonly TimeProvider _timeProvider;

    public ParticipantManagementService(
        IGameRepository gameRepository,
        IParticipantRepository participantRepository,
        IElementRepository elementRepository,
        IUnitOfWork unitOfWork,
        IParticipantTokenService tokenService,
        IProfanityFilter profanityFilter,
        TimeProvider timeProvider)
    {
        _gameRepository = gameRepository;
        _participantRepository = participantRepository;
        _elementRepository = elementRepository;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _profanityFilter = profanityFilter;
        _timeProvider = timeProvider;
    }

    public async Task<string> JoinGameAsync(JoinGameCommand command, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(command.GameId, cancellationToken);
        if (game == null)
            throw new ArgumentException($"Game {command.GameId} not found.");

        if (game.State != GameState.Open && game.State != GameState.Live)
            throw new InvalidOperationException("Participants can only join when the game is Open or Live.");

        if (_profanityFilter.ContainsProfanity(command.Nickname))
            throw new ArgumentException("The chosen nickname is not allowed.");

        var existing = await _participantRepository.GetByNicknameAsync(command.GameId, command.Nickname, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException("This nickname is already taken.");

        var participantId = Guid.NewGuid();
        var (token, hash) = _tokenService.GenerateToken(participantId, command.GameId, command.Nickname);

        var participant = new Participant(participantId, command.GameId, command.Nickname, hash, _timeProvider.GetUtcNow());
        _participantRepository.Add(participant);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return token;
    }

    public async Task<Roster.Application.Participants.Queries.ParticipantProfileDto?> GetParticipantProfileAsync(Roster.Application.Participants.Queries.GetParticipantProfileQuery query, CancellationToken cancellationToken = default)
    {
        var participant = await _participantRepository.GetByIdAsync(query.ParticipantId, cancellationToken);
        if (participant == null || participant.GameId != query.GameId)
            return null;

        // In a real app we would compute or read rank and total score. For now, just return defaults.
        return new Roster.Application.Participants.Queries.ParticipantProfileDto(
            participant.Id,
            participant.GameId,
            participant.Nickname,
            null, // Rank not yet implemented
            0m    // Total score not yet implemented
        );
    }

    public async Task<Roster.Application.Participants.Queries.ParticipantLineupDto?> GetParticipantLineupAsync(Roster.Application.Participants.Queries.GetParticipantLineupQuery query, CancellationToken cancellationToken = default)
    {
        var participant = await _participantRepository.GetByIdAsync(query.ParticipantId, cancellationToken);
        if (participant == null || participant.GameId != query.GameId)
            return null;

        var lineup = await _participantRepository.GetLineupByParticipantIdAsync(query.ParticipantId, cancellationToken);
        if (lineup == null)
            return null;

        return new Roster.Application.Participants.Queries.ParticipantLineupDto(
            lineup.ParticipantId,
            lineup.PickedElementIds,
            lineup.CaptainElementId,
            lineup.SubmittedAt,
            lineup.IsLocked
        );
    }

    public async Task SaveLineupAsync(SaveLineupCommand command, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(command.GameId, cancellationToken);
        if (game == null)
            throw new ArgumentException($"Game {command.GameId} not found.");

        if (game.State != GameState.Open)
            throw new InvalidOperationException("Lineups can only be edited when the game is Open.");

        var participant = await _participantRepository.GetByIdAsync(command.ParticipantId, cancellationToken);
        if (participant == null || participant.GameId != command.GameId)
            throw new ArgumentException("Participant not found or does not belong to this game.");

        var pickedElementIds = command.PickedElementIds ?? Array.Empty<Guid>();
        if (pickedElementIds.Length != game.LineupSize)
            throw new InvalidOperationException($"Lineup must have exactly {game.LineupSize} elements.");

        var uniqueIds = pickedElementIds.Distinct().ToList();
        if (uniqueIds.Count != pickedElementIds.Length)
            throw new InvalidOperationException("Lineup contains duplicate elements.");

        if (game.CaptainEnabled)
        {
            if (command.CaptainElementId == null)
                throw new InvalidOperationException("A captain must be selected.");
            if (!pickedElementIds.Contains(command.CaptainElementId.Value))
                throw new InvalidOperationException("The selected captain is not part of the lineup.");
        }
        else
        {
            if (command.CaptainElementId != null)
                throw new InvalidOperationException("Captain selection is not enabled for this game.");
        }

        var elements = await _elementRepository.GetByGameIdAsync(command.GameId, cancellationToken);
        var availableElements = elements.Where(e => !e.IsHidden && e.IsSelectable).ToDictionary(e => e.Id);

        foreach (var elementId in pickedElementIds)
        {
            if (!availableElements.ContainsKey(elementId))
                throw new InvalidOperationException($"Element {elementId} is not available for selection.");
        }

        var lineup = await _participantRepository.GetLineupByParticipantIdAsync(command.ParticipantId, cancellationToken);
        if (lineup == null)
        {
            lineup = new Lineup(command.ParticipantId, pickedElementIds, command.CaptainElementId, _timeProvider.GetUtcNow());
            _participantRepository.AddLineup(lineup);
        }
        else
        {
            if (lineup.IsLocked)
                throw new InvalidOperationException("Cannot update a locked lineup.");
            lineup.UpdatePicks(pickedElementIds, command.CaptainElementId, _timeProvider.GetUtcNow());
            _participantRepository.UpdateLineup(lineup);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
