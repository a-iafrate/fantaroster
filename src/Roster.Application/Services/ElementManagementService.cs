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

    public async Task UpdateElementDetailsAsync(Guid elementId, string name, string? subtitle, string? group, CancellationToken cancellationToken = default)
    {
        var element = await _elementRepository.GetByIdAsync(elementId, cancellationToken);
        if (element == null) throw new ArgumentException($"Element {elementId} not found.");

        element.UpdateDetails(name, subtitle, group);
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
