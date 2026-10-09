using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roster.Application.Ports.Data;
using Roster.Domain.Elements;

namespace Roster.Application.Services;

public sealed class ElementManagementService
{
    private readonly IElementRepository _elementRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ElementManagementService(IElementRepository elementRepository, IUnitOfWork unitOfWork)
    {
        _elementRepository = elementRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<Element>> GetElementsByGameAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        return await _elementRepository.GetByGameIdAsync(gameId, cancellationToken);
    }

    public async Task<IReadOnlyList<Roster.Application.Elements.Queries.AvailableElementDto>> GetAvailableElementsAsync(Roster.Application.Elements.Queries.GetAvailableElementsQuery query, CancellationToken cancellationToken = default)
    {
        var elements = await _elementRepository.GetByGameIdAsync(query.GameId, cancellationToken);
        var availableElements = new List<Roster.Application.Elements.Queries.AvailableElementDto>();

        foreach (var element in elements)
        {
            if (element.IsHidden)
                continue;

            if (!element.IsSelectable)
                continue;

            availableElements.Add(new Roster.Application.Elements.Queries.AvailableElementDto(
                element.Id,
                element.Name,
                element.Subtitle,
                element.Group,
                element.ImageUrl
            ));
        }

        return availableElements;
    }

    public async Task<Guid> AddElementAsync(Guid gameId, string name, string? subtitle, string? group, string? imageUrl, CancellationToken cancellationToken = default)
    {
        var element = new Element(
            id: Guid.NewGuid(),
            gameId: gameId,
            sourceBindingId: null, // manual elements have no source binding
            externalId: Guid.NewGuid().ToString(), // unique placeholder
            name: name,
            subtitle: subtitle,
            imageUrl: imageUrl,
            group: group,
            consentStatus: ConsentStatus.NotRequired // By default, could be updated if needed
        );

        _elementRepository.Add(element);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return element.Id;
    }

    public async Task UpdateElementDetailsAsync(Guid elementId, string name, string? subtitle, string? group, string? imageUrl, CancellationToken cancellationToken = default)
    {
        var element = await _elementRepository.GetByIdAsync(elementId, cancellationToken);
        if (element == null) throw new ArgumentException($"Element {elementId} not found.");

        element.UpdateDetails(name, subtitle, group, imageUrl);
        _elementRepository.Update(element);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetElementSelectabilityAsync(Guid elementId, bool isSelectable, CancellationToken cancellationToken = default)
    {
        var element = await _elementRepository.GetByIdAsync(elementId, cancellationToken);
        if (element == null) throw new ArgumentException($"Element {elementId} not found.");

        element.SetSelectability(isSelectable);
        _elementRepository.Update(element);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetElementVisibilityAsync(Guid elementId, bool isHidden, CancellationToken cancellationToken = default)
    {
        var element = await _elementRepository.GetByIdAsync(elementId, cancellationToken);
        if (element == null) throw new ArgumentException($"Element {elementId} not found.");

        element.SetVisibility(isHidden);
        _elementRepository.Update(element);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
