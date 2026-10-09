using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roster.Application.Ports.Data;
using Roster.Domain.Games;

namespace Roster.Application.Services;

public sealed class RuleManagementService
{
    private readonly IRuleRepository _ruleRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RuleManagementService(IRuleRepository ruleRepository, IGameRepository gameRepository, IUnitOfWork unitOfWork)
    {
        _ruleRepository = ruleRepository;
        _gameRepository = gameRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<Rule>> GetRulesByGameAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var rules = await _ruleRepository.GetByGameIdAsync(gameId, cancellationToken);
        return rules.OrderBy(r => r.Order).ThenBy(r => r.Id).ToList();
    }

    public async Task AddRuleAsync(Guid gameId, string label, int points, string category, RuleTarget target, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);
        if (game == null) throw new ArgumentException($"Game {gameId} not found.");

        if (game.State != GameState.Draft && game.State != GameState.Open)
        {
            throw new InvalidOperationException("Rules can only be added when game is in Draft or Open state.");
        }

        var currentRules = await _ruleRepository.GetByGameIdAsync(gameId, cancellationToken);
        var maxOrder = currentRules.Count > 0 ? currentRules.Max(r => r.Order) : 0;

        var rule = new Rule(Guid.NewGuid(), gameId, label, points, category, target, maxOrder + 1);
        _ruleRepository.Add(rule);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task EditRuleAsync(Guid gameId, Guid ruleId, string label, int points, string category, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);
        if (game == null) throw new ArgumentException($"Game {gameId} not found.");

        if (game.State != GameState.Draft && game.State != GameState.Open)
        {
            throw new InvalidOperationException("Rules can only be edited when game is in Draft or Open state.");
        }

        var rule = await _ruleRepository.GetByIdAsync(ruleId, cancellationToken);
        if (rule == null || rule.GameId != gameId) throw new ArgumentException($"Rule {ruleId} not found or doesn't belong to the game.");

        rule.Update(label, points, category);
        _ruleRepository.Update(rule);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRuleAsync(Guid gameId, Guid ruleId, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);
        if (game == null) throw new ArgumentException($"Game {gameId} not found.");

        if (game.State != GameState.Draft && game.State != GameState.Open)
        {
            throw new InvalidOperationException("Rules can only be deleted when game is in Draft or Open state.");
        }

        var rule = await _ruleRepository.GetByIdAsync(ruleId, cancellationToken);
        if (rule == null || rule.GameId != gameId) throw new ArgumentException($"Rule {ruleId} not found or doesn't belong to the game.");

        _ruleRepository.Remove(rule);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ReorderRulesAsync(Guid gameId, IReadOnlyList<Guid> ruleIdsInOrder, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);
        if (game == null) throw new ArgumentException($"Game {gameId} not found.");

        if (game.State != GameState.Draft && game.State != GameState.Open)
        {
            throw new InvalidOperationException("Rules can only be reordered when game is in Draft or Open state.");
        }

        var currentRules = await _ruleRepository.GetByGameIdAsync(gameId, cancellationToken);

        for (int i = 0; i < ruleIdsInOrder.Count; i++)
        {
            var rule = currentRules.FirstOrDefault(r => r.Id == ruleIdsInOrder[i]);
            if (rule != null)
            {
                rule.SetOrder(i);
                _ruleRepository.Update(rule);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
