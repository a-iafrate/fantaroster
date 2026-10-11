using System.Security.Cryptography;
using Roster.Application.Ports.Data;
using Roster.Domain.Elements;
using Roster.Domain.Games;
using Roster.Domain.Sources;

namespace Roster.Application.Services;

/// <summary>
/// Starts a new edition from an existing game: same settings, rules, data sources and elements; no participants,
/// lineups or points. People in play who had to consent must consent again, because a yes is given for one edition.
/// </summary>
public sealed class GameDuplicationService(
    IGameRepository games,
    IRuleRepository rules,
    IElementRepository elements,
    ISourceBindingRepository bindings,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    /// <returns>The id of the new game, or <c>null</c> when the source game does not exist or belongs to someone else.</returns>
    public async Task<Guid?> DuplicateAsync(Guid sourceGameId, string organizerId, string newName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);

        var source = await games.GetByIdAsync(sourceGameId, cancellationToken);
        if (source is null || !source.IsOwnedBy(organizerId))
        {
            return null;
        }

        var newId = Guid.NewGuid();
        var copy = new Game(
            newId,
            GenerateSlug(newName),
            newName,
            source.DomainPackId,
            source.Brand,
            source.Culture,
            source.LineupSize,
            source.CaptainEnabled,
            source.CaptainMultiplier,
            GenerateJoinCode(),
            timeProvider.GetUtcNow());
        copy.AssignToOrganizer(organizerId);
        games.Add(copy);

        foreach (var rule in await rules.GetByGameIdAsync(sourceGameId, cancellationToken))
        {
            rules.Add(new Rule(Guid.NewGuid(), newId, rule.Label, rule.Points, rule.Category, rule.Target, rule.Order));
        }

        var bindingIds = new Dictionary<Guid, Guid>();
        foreach (var binding in await bindings.GetByGameIdAsync(sourceGameId, cancellationToken))
        {
            var id = Guid.NewGuid();
            bindingIds[binding.Id] = id;
            bindings.Add(new SourceBinding(id, newId, binding.PluginId, binding.ConfigurationJson, binding.SecretReference));
        }

        foreach (var element in await elements.GetByGameIdAsync(sourceGameId, cancellationToken))
        {
            if (element.SourceState == SourceState.MissingFromSource)
            {
                continue;
            }

            Guid? bindingId = element.SourceBindingId is { } old && bindingIds.TryGetValue(old, out var mapped) ? mapped : null;
            var consent = element.ConsentStatus == ConsentStatus.NotRequired ? ConsentStatus.NotRequired : ConsentStatus.Pending;
            var clone = new Element(Guid.NewGuid(), newId, bindingId, element.ExternalId, element.Name, element.Subtitle, element.ImageUrl, element.Group, consent);
            clone.SetVisibility(element.IsHidden);
            elements.Add(clone);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return newId;
    }

    private static string GenerateJoinCode()
    {
        const string chars = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
        Span<char> result = stackalloc char[5];
        foreach (ref var c in result)
        {
            c = chars[RandomNumberGenerator.GetInt32(chars.Length)];
        }

        return new string(result);
    }

    private static string GenerateSlug(string name)
    {
        var letters = name.Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray();
        return new string(letters).Replace(' ', '-').ToLowerInvariant() + "-" + RandomNumberGenerator.GetInt32(1000, 9999);
    }
}
