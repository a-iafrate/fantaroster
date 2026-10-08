import os

base_path = "src/Roster.Domain"
folders = ["Games", "Elements", "Participants", "Scores", "Sources", "Referees"]
for f in folders:
    os.makedirs(f"{base_path}/{f}", exist_ok=True)

def write_file(path, content):
    with open(f"{base_path}/{path}", "w", encoding="utf-8") as f:
        f.write(content)

# --- Games ---
write_file("Games/GameState.cs", """namespace Roster.Domain.Games;

public enum GameState
{
    Draft,
    Open,
    Live,
    Ended,
    Archived
}
""")

write_file("Games/RuleTarget.cs", """namespace Roster.Domain.Games;

public enum RuleTarget
{
    Element,
    Participant
}
""")

write_file("Games/Rule.cs", """using System;

namespace Roster.Domain.Games;

public sealed class Rule
{
    public Guid Id { get; private set; }
    public Guid GameId { get; private set; }
    public string Label { get; private set; }
    public int Points { get; private set; }
    public string Category { get; private set; }
    public RuleTarget Target { get; private set; }

    private Rule() 
    { 
        Label = string.Empty;
        Category = string.Empty;
    }

    public Rule(Guid id, Guid gameId, string label, int points, string category, RuleTarget target)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        if (points == 0) throw new ArgumentException("Points must be a non-zero integer.", nameof(points));

        Id = id;
        GameId = gameId;
        Label = label;
        Points = points;
        Category = category;
        Target = target;
    }

    public void Update(string label, int points, string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        if (points == 0) throw new ArgumentException("Points must be a non-zero integer.", nameof(points));

        Label = label;
        Points = points;
        Category = category;
    }
}
""")

write_file("Games/Game.cs", """using System;

namespace Roster.Domain.Games;

public sealed class Game
{
    public Guid Id { get; private set; }
    public string Slug { get; private set; }
    public string Name { get; private set; }
    public string DomainPackId { get; private set; }
    public string Brand { get; private set; }
    public string Culture { get; private set; }
    public GameState State { get; private set; }
    public int LineupSize { get; private set; }
    public bool CaptainEnabled { get; private set; }
    public decimal CaptainMultiplier { get; private set; }
    public string JoinCode { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public byte[] RowVersion { get; private set; }

    private Game() 
    { 
        Slug = string.Empty;
        Name = string.Empty;
        DomainPackId = string.Empty;
        Brand = string.Empty;
        Culture = string.Empty;
        JoinCode = string.Empty;
        RowVersion = Array.Empty<byte>();
    }

    public Game(Guid id, string slug, string name, string domainPackId, string brand, string culture, int lineupSize, bool captainEnabled, decimal captainMultiplier, string joinCode, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(joinCode);

        Id = id;
        Slug = slug;
        Name = name;
        DomainPackId = domainPackId;
        Brand = brand;
        Culture = culture;
        State = GameState.Draft;
        LineupSize = lineupSize;
        CaptainEnabled = captainEnabled;
        CaptainMultiplier = captainMultiplier;
        JoinCode = joinCode;
        CreatedAt = createdAt;
        RowVersion = Array.Empty<byte>();
    }

    public void Open()
    {
        if (State != GameState.Draft) throw new InvalidOperationException("Game must be in Draft state to be opened.");
        State = GameState.Open;
    }

    public void GoLive(DateTimeOffset startedAt)
    {
        if (State != GameState.Open) throw new InvalidOperationException("Game must be in Open state to go live.");
        State = GameState.Live;
        StartedAt = startedAt;
    }

    public void End(DateTimeOffset endedAt)
    {
        if (State != GameState.Live) throw new InvalidOperationException("Game must be in Live state to end.");
        State = GameState.Ended;
        EndedAt = endedAt;
    }

    public void Archive()
    {
        if (State != GameState.Ended) throw new InvalidOperationException("Game must be in Ended state to be archived.");
        State = GameState.Archived;
    }
}
""")

# --- Elements ---
write_file("Elements/ConsentStatus.cs", """namespace Roster.Domain.Elements;

public enum ConsentStatus
{
    NotRequired,
    Pending,
    Accepted,
    Declined
}
""")

write_file("Elements/SourceState.cs", """namespace Roster.Domain.Elements;

public enum SourceState
{
    Active,
    MissingFromSource
}
""")

write_file("Elements/Element.cs", """using System;
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
}
""")

write_file("Elements/ConsentInvitation.cs", """using System;

namespace Roster.Domain.Elements;

public sealed class ConsentInvitation
{
    public Guid Id { get; private set; }
    public Guid ElementId { get; private set; }
    public string TokenHash { get; private set; }
    public string? Contact { get; private set; }
    public DateTimeOffset SentAt { get; private set; }
    public DateTimeOffset? AnsweredAt { get; private set; }

    private ConsentInvitation() 
    { 
        TokenHash = string.Empty;
    }

    public ConsentInvitation(Guid id, Guid elementId, string tokenHash, string? contact, DateTimeOffset sentAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        
        Id = id;
        ElementId = elementId;
        TokenHash = tokenHash;
        Contact = contact;
        SentAt = sentAt;
    }

    public void MarkAnswered(DateTimeOffset answeredAt)
    {
        AnsweredAt = answeredAt;
    }
}
""")

# --- Participants ---
write_file("Participants/Participant.cs", """using System;

namespace Roster.Domain.Participants;

public sealed class Participant
{
    public Guid Id { get; private set; }
    public Guid GameId { get; private set; }
    public string Nickname { get; private set; }
    public string TokenHash { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }

    private Participant() 
    { 
        Nickname = string.Empty;
        TokenHash = string.Empty;
    }

    public Participant(Guid id, Guid gameId, string nickname, string tokenHash, DateTimeOffset joinedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nickname);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        Id = id;
        GameId = gameId;
        Nickname = nickname;
        TokenHash = tokenHash;
        JoinedAt = joinedAt;
    }
}
""")

write_file("Participants/Lineup.cs", """using System;
using System.Collections.Generic;

namespace Roster.Domain.Participants;

public sealed class Lineup
{
    public Guid ParticipantId { get; private set; }
    public Guid[] PickedElementIds { get; private set; }
    public Guid? CaptainElementId { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public bool IsLocked { get; private set; }

    private Lineup() 
    { 
        PickedElementIds = Array.Empty<Guid>();
    }

    public Lineup(Guid participantId, Guid[] pickedElementIds, Guid? captainElementId, DateTimeOffset submittedAt)
    {
        ParticipantId = participantId;
        PickedElementIds = pickedElementIds ?? Array.Empty<Guid>();
        CaptainElementId = captainElementId;
        SubmittedAt = submittedAt;
        IsLocked = false;
    }

    public void Lock()
    {
        IsLocked = true;
    }
    
    public void UpdatePicks(Guid[] pickedElementIds, Guid? captainElementId, DateTimeOffset submittedAt)
    {
        if (IsLocked) throw new InvalidOperationException("Cannot update a locked lineup.");
        
        PickedElementIds = pickedElementIds ?? Array.Empty<Guid>();
        CaptainElementId = captainElementId;
        SubmittedAt = submittedAt;
    }
}
""")

# --- Scores ---
write_file("Scores/ScoreStatus.cs", """namespace Roster.Domain.Scores;

public enum ScoreStatus
{
    Valid,
    Voided
}
""")

write_file("Scores/ScoreSourceEnum.cs", """namespace Roster.Domain.Scores;

public enum ScoreSourceEnum
{
    Referee,
    ReportApproval,
    Sponsor,
    Plugin
}
""")

write_file("Scores/ReportStatus.cs", """namespace Roster.Domain.Scores;

public enum ReportStatus
{
    Pending,
    Approved,
    Rejected
}
""")

write_file("Scores/ScoreEntry.cs", """using System;

namespace Roster.Domain.Scores;

public sealed class ScoreEntry
{
    public Guid Id { get; private set; }
    public Guid GameId { get; private set; }
    public Guid? ElementId { get; private set; }
    public Guid? ParticipantId { get; private set; }
    public Guid RuleId { get; private set; }
    public int PointsSnapshot { get; private set; }
    public ScoreSourceEnum Source { get; private set; }
    public string CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string IdempotencyKey { get; private set; }
    public ScoreStatus Status { get; private set; }
    public string? VoidedBy { get; private set; }
    public DateTimeOffset? VoidedAt { get; private set; }
    public string? Note { get; private set; }

    private ScoreEntry() 
    { 
        CreatedBy = string.Empty;
        IdempotencyKey = string.Empty;
    }

    public ScoreEntry(Guid id, Guid gameId, Guid? elementId, Guid? participantId, Guid ruleId, int pointsSnapshot, ScoreSourceEnum source, string createdBy, DateTimeOffset createdAt, string idempotencyKey, string? note = null)
    {
        if (elementId == null && participantId == null) throw new ArgumentException("Either ElementId or ParticipantId must be provided.");
        if (pointsSnapshot == 0) throw new ArgumentException("Points snapshot must be non-zero.");
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        Id = id;
        GameId = gameId;
        ElementId = elementId;
        ParticipantId = participantId;
        RuleId = ruleId;
        PointsSnapshot = pointsSnapshot;
        Source = source;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        IdempotencyKey = idempotencyKey;
        Status = ScoreStatus.Valid;
        Note = note;
    }

    public void Void(string voidedBy, DateTimeOffset voidedAt)
    {
        if (Status == ScoreStatus.Voided) return;
        ArgumentException.ThrowIfNullOrWhiteSpace(voidedBy);
        
        Status = ScoreStatus.Voided;
        VoidedBy = voidedBy;
        VoidedAt = voidedAt;
    }
}
""")

write_file("Scores/Report.cs", """using System;

namespace Roster.Domain.Scores;

public sealed class Report
{
    public Guid Id { get; private set; }
    public Guid GameId { get; private set; }
    public Guid ParticipantId { get; private set; }
    public Guid ElementId { get; private set; }
    public Guid? SuggestedRuleId { get; private set; }
    public string? Text { get; private set; }
    public string? PhotoBlobName { get; private set; }
    public ReportStatus Status { get; private set; }
    public Guid? ResultingScoreEntryId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Report() { }

    public Report(Guid id, Guid gameId, Guid participantId, Guid elementId, Guid? suggestedRuleId, string? text, string? photoBlobName, DateTimeOffset createdAt)
    {
        Id = id;
        GameId = gameId;
        ParticipantId = participantId;
        ElementId = elementId;
        SuggestedRuleId = suggestedRuleId;
        Text = text;
        PhotoBlobName = photoBlobName;
        Status = ReportStatus.Pending;
        CreatedAt = createdAt;
    }

    public void Approve(Guid scoreEntryId)
    {
        if (Status != ReportStatus.Pending) throw new InvalidOperationException("Only pending reports can be approved.");
        Status = ReportStatus.Approved;
        ResultingScoreEntryId = scoreEntryId;
    }

    public void Reject()
    {
        if (Status != ReportStatus.Pending) throw new InvalidOperationException("Only pending reports can be rejected.");
        Status = ReportStatus.Rejected;
    }
}
""")

write_file("Scores/SponsorBonus.cs", """using System;

namespace Roster.Domain.Scores;

public sealed class SponsorBonus
{
    public Guid Id { get; private set; }
    public Guid GameId { get; private set; }
    public string SponsorName { get; private set; }
    public int Points { get; private set; }
    public string QrToken { get; private set; }
    public int ScanCount { get; private set; }

    private SponsorBonus() 
    { 
        SponsorName = string.Empty;
        QrToken = string.Empty;
    }

    public SponsorBonus(Guid id, Guid gameId, string sponsorName, int points, string qrToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sponsorName);
        ArgumentException.ThrowIfNullOrWhiteSpace(qrToken);
        if (points == 0) throw new ArgumentException("Points must be non-zero.");

        Id = id;
        GameId = gameId;
        SponsorName = sponsorName;
        Points = points;
        QrToken = qrToken;
        ScanCount = 0;
    }

    public void IncrementScan()
    {
        ScanCount++;
    }
}
""")

# --- Sources ---
write_file("Sources/SourceBinding.cs", """using System;

namespace Roster.Domain.Sources;

public sealed class SourceBinding
{
    public Guid Id { get; private set; }
    public Guid GameId { get; private set; }
    public string PluginId { get; private set; }
    public string ConfigurationJson { get; private set; }
    public string? SecretReference { get; private set; }
    public DateTimeOffset? LastSyncTime { get; private set; }
    public string? LastSyncResult { get; private set; }

    private SourceBinding() 
    { 
        PluginId = string.Empty;
        ConfigurationJson = "{}";
    }

    public SourceBinding(Guid id, Guid gameId, string pluginId, string configurationJson, string? secretReference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);

        Id = id;
        GameId = gameId;
        PluginId = pluginId;
        ConfigurationJson = configurationJson;
        SecretReference = secretReference;
    }

    public void RecordSync(DateTimeOffset syncTime, string result)
    {
        LastSyncTime = syncTime;
        LastSyncResult = result;
    }
}
""")

# --- Referees ---
write_file("Referees/Referee.cs", """using System;

namespace Roster.Domain.Referees;

public sealed class Referee
{
    public Guid Id { get; private set; }
    public Guid GameId { get; private set; }
    public string DisplayName { get; private set; }
    public string InviteTokenHash { get; private set; }
    public string[] Scope { get; private set; }

    private Referee() 
    { 
        DisplayName = string.Empty;
        InviteTokenHash = string.Empty;
        Scope = Array.Empty<string>();
    }

    public Referee(Guid id, Guid gameId, string displayName, string inviteTokenHash, string[] scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(inviteTokenHash);

        Id = id;
        GameId = gameId;
        DisplayName = displayName;
        InviteTokenHash = inviteTokenHash;
        Scope = scope ?? Array.Empty<string>();
    }
}
""")

print("Domain generation complete!")
