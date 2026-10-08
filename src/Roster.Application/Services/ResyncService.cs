using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Roster.Application.Ports;
using Roster.Application.Ports.Data;
using Roster.Domain.Elements;
using Roster.Plugins.Abstractions;

namespace Roster.Application.Services;

public sealed class ResyncService : IResyncService
{
    private readonly ISourceBindingRepository _sourceBindingRepository;
    private readonly IElementRepository _elementRepository;
    private readonly IPluginRegistry _pluginRegistry;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ResyncService(
        ISourceBindingRepository sourceBindingRepository,
        IElementRepository elementRepository,
        IPluginRegistry pluginRegistry,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _sourceBindingRepository = sourceBindingRepository ?? throw new ArgumentNullException(nameof(sourceBindingRepository));
        _elementRepository = elementRepository ?? throw new ArgumentNullException(nameof(elementRepository));
        _pluginRegistry = pluginRegistry ?? throw new ArgumentNullException(nameof(pluginRegistry));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ResyncSummary> ResyncGameElementsAsync(Guid gameId, Guid sourceBindingId, CancellationToken cancellationToken)
    {
        var binding = await _sourceBindingRepository.GetByIdAsync(sourceBindingId, cancellationToken);
        if (binding == null || binding.GameId != gameId)
        {
            throw new ArgumentException("Source binding not found for the specified game.");
        }

        var plugin = _pluginRegistry.GetPlugin(binding.PluginId);
        if (plugin == null)
        {
            throw new InvalidOperationException($"Plugin '{binding.PluginId}' is not registered.");
        }

        var configValues = JsonSerializer.Deserialize<Dictionary<string, string>>(binding.ConfigurationJson)
            ?? new Dictionary<string, string>();
        var config = new PluginConfig(configValues);

        var importResult = await plugin.ImportAsync(config, cancellationToken);
        var importedElements = importResult.Elements;

        var existingElements = await _elementRepository.GetByGameIdAsync(gameId, cancellationToken);
        var existingByExternalId = existingElements
            .Where(e => e.SourceBindingId == sourceBindingId && !string.IsNullOrEmpty(e.ExternalId))
            .ToDictionary(e => e.ExternalId!);

        int addedCount = 0;
        int updatedCount = 0;
        int missingCount = 0;

        var processedExternalIds = new HashSet<string>();

        foreach (var imported in importedElements)
        {
            if (string.IsNullOrEmpty(imported.ExternalId))
            {
                continue; // Can't resync without ExternalId
            }

            processedExternalIds.Add(imported.ExternalId);

            if (existingByExternalId.TryGetValue(imported.ExternalId, out var existingElement))
            {
                // Update
                existingElement.UpdateFromSource(
                    name: imported.Name,
                    subtitle: imported.Subtitle,
                    imageUrl: imported.ImageUrl,
                    group: imported.Group
                );
                _elementRepository.Update(existingElement);
                updatedCount++;
            }
            else
            {
                // Add
                // Note: For now, default to NotRequired. Later this should come from DomainPack.
                var newElement = new Element(
                    id: Guid.NewGuid(),
                    gameId: gameId,
                    sourceBindingId: sourceBindingId,
                    externalId: imported.ExternalId,
                    name: imported.Name,
                    subtitle: imported.Subtitle,
                    imageUrl: imported.ImageUrl,
                    group: imported.Group,
                    consentStatus: ConsentStatus.NotRequired
                );
                _elementRepository.Add(newElement);
                addedCount++;
            }
        }

        // Mark missing
        foreach (var existing in existingElements.Where(e => e.SourceBindingId == sourceBindingId && e.SourceState == SourceState.Active))
        {
            if (!string.IsNullOrEmpty(existing.ExternalId) && !processedExternalIds.Contains(existing.ExternalId))
            {
                existing.MarkMissingFromSource();
                _elementRepository.Update(existing);
                missingCount++;
            }
        }

        // Record sync time
        binding.RecordSync(_timeProvider.GetUtcNow(), "Success");
        _sourceBindingRepository.Update(binding);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ResyncSummary(addedCount, updatedCount, missingCount, importResult.Warnings);
    }
}
