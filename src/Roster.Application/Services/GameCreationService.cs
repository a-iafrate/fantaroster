using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Roster.Application.Ports;
using Roster.Application.Ports.Data;
using Roster.Domain.Games;
using Roster.Domain.Sources;
using Roster.DomainPacks;

namespace Roster.Application.Services;

public sealed class GameCreationService : IGameCreationService
{
    private readonly IGameRepository _gameRepository;
    private readonly IRuleRepository _ruleRepository;
    private readonly ISourceBindingRepository _sourceBindingRepository;
    private readonly IDomainPackLoader _domainPackLoader;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly IResyncService _resyncService;

    public GameCreationService(
        IGameRepository gameRepository,
        IRuleRepository ruleRepository,
        ISourceBindingRepository sourceBindingRepository,
        IDomainPackLoader domainPackLoader,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IResyncService resyncService)
    {
        _gameRepository = gameRepository;
        _ruleRepository = ruleRepository;
        _sourceBindingRepository = sourceBindingRepository;
        _domainPackLoader = domainPackLoader;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _resyncService = resyncService;
    }

    public async Task<Guid> CreateGameAsync(CreateGameCommand command, CancellationToken cancellationToken)
    {
        var pack = _domainPackLoader.GetPack(command.DomainPackId);
        if (pack == null) throw new ArgumentException($"Domain pack {command.DomainPackId} not found.");

        var gameId = Guid.NewGuid();
        var slug = GenerateSlug(command.Name);
        var joinCode = GenerateJoinCode();

        var game = new Game(
            gameId,
            slug,
            command.Name,
            command.DomainPackId,
            command.Brand,
            command.Culture,
            command.LineupSize,
            command.CaptainEnabled,
            command.CaptainMultiplier,
            joinCode,
            _timeProvider.GetUtcNow()
        );

        _gameRepository.Add(game);

        // Add rules from domain pack
        if (pack.DefaultRules != null)
        {
            foreach (var ruleDef in pack.DefaultRules)
            {
                // Fallback to first available language if culture not matched perfectly
                var label = ruleDef.Label.TryGetValue(command.Culture, out var l) ? l : ruleDef.Label.Values.FirstOrDefault() ?? "Rule";
                var rule = new Rule(Guid.NewGuid(), gameId, label, ruleDef.Points, string.Empty, RuleTarget.Element);
                _ruleRepository.Add(rule);
            }
        }

        Guid? sourceBindingId = null;

        if (!string.IsNullOrEmpty(command.PluginId))
        {
            sourceBindingId = Guid.NewGuid();
            var binding = new SourceBinding(
                sourceBindingId.Value,
                gameId,
                command.PluginId,
                command.PluginConfig != null ? JsonSerializer.Serialize(command.PluginConfig) : "{}",
                null // secretReference
            );
            _sourceBindingRepository.Add(binding);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (sourceBindingId.HasValue)
        {
            await _resyncService.ResyncGameElementsAsync(gameId, sourceBindingId.Value, cancellationToken);
        }

        return gameId;
    }

    private static string GenerateJoinCode()
    {
        const string chars = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ"; // No 0, 1, O, I
        Span<char> result = stackalloc char[5];
        foreach (ref char c in result)
        {
            c = chars[RandomNumberGenerator.GetInt32(chars.Length)];
        }
        return new string(result);
    }

    private static string GenerateSlug(string name)
    {
        // Very basic slug generation
        var arr = name.Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray();
        return new string(arr).Replace(' ', '-').ToLowerInvariant() + "-" + RandomNumberGenerator.GetInt32(1000, 9999);
    }
}
