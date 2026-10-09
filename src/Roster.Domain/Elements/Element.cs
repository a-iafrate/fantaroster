using System;
using System.Collections.Generic;

namespace Roster.Domain.Elements;

public sealed class Element
{
    public Guid Id { get; private set; }
    public Guid GameId { get; private set; }
    public Guid? SourceBindingId { get; private set; }
    public string? ExternalId { get; private set; }
    public string Name { get; private set; }
    public string? Subtitle { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? Group { get; private set; }
    public string MetadataJson { get; private set; }
    public ConsentStatus ConsentStatus { get; private set; }
    public SourceState SourceState { get; private set; }
    public bool IsSelectable { get; private set; }
    public bool IsHidden { get; private set; }

    private Element()
    {
        Name = string.Empty;
        MetadataJson = "{}";
    }

    public Element(Guid id, Guid gameId, Guid? sourceBindingId, string? externalId, string name, string? subtitle, string? imageUrl, string? group, ConsentStatus consentStatus)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        GameId = gameId;
        SourceBindingId = sourceBindingId;
        ExternalId = externalId;
        Name = name;
        Subtitle = subtitle;
        ImageUrl = imageUrl;
        Group = group;
        MetadataJson = "{}";
        ConsentStatus = consentStatus;
        SourceState = SourceState.Active;
        IsSelectable = consentStatus == ConsentStatus.NotRequired || consentStatus == ConsentStatus.Accepted;
    }

    public void UpdateFromSource(string name, string? subtitle, string? imageUrl, string? group)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Subtitle = subtitle;
        ImageUrl = imageUrl;
        Group = group;
        SourceState = SourceState.Active;
    }

    public void MarkMissingFromSource()
    {
        SourceState = SourceState.MissingFromSource;
    }

    public void UpdateConsent(ConsentStatus status)
    {
        ConsentStatus = status;
        IsSelectable = status == ConsentStatus.NotRequired || status == ConsentStatus.Accepted;
    }

    public void SetSelectability(bool isSelectable)
    {
        IsSelectable = isSelectable;
    }

    public void SetVisibility(bool isHidden)
    {
        IsHidden = isHidden;
    }

    public void UpdateDetails(string name, string? subtitle, string? group, string? imageUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Subtitle = subtitle;
        Group = group;
        ImageUrl = imageUrl;
    }
}
