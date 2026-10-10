using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Roster.Web.Client.Services;

/// <summary>
/// State of the participant experience for one game: game info, profile, lineup, leaderboard and feed.
/// Loaded once and kept current through the game hub, so switching tabs does not refetch everything.
/// In WebAssembly a scoped service lives for the whole app, so every participant page shares it.
/// </summary>
public sealed class ParticipantSession : IDisposable
{
    private readonly HttpClient _http;
    private readonly ParticipantAuthState _auth;
    private readonly GameHubClient _hub;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private Dictionary<Guid, int> _previousRanks = [];
    private bool _hubAttached;

    public ParticipantSession(HttpClient http, ParticipantAuthState auth, GameHubClient hub)
    {
        _http = http;
        _auth = auth;
        _hub = hub;
    }

    public Guid GameId { get; private set; }

    public ParticipantSessionStatus Status { get; private set; } = ParticipantSessionStatus.Loading;

    public GameInfo? Game { get; private set; }

    public ParticipantProfile? Profile { get; private set; }

    public ParticipantLineup? Lineup { get; private set; }

    public LeaderboardResponse? Leaderboard { get; private set; }

    public IReadOnlyList<FeedItem> Feed { get; private set; } = [];

    public IReadOnlyList<AvailableElement>? Elements { get; private set; }

    public IReadOnlyList<GameRule>? Rules { get; private set; }

    /// <summary>Rank movement since the previous leaderboard: positive means places gained.</summary>
    public IReadOnlyDictionary<Guid, int> RankChanges { get; private set; } = new Dictionary<Guid, int>();

    public bool HasUnseenPoints { get; private set; }

    public int? MyRank => Leaderboard?.MyPosition?.Rank ?? Profile?.Rank;

    public int MyScore => Leaderboard?.MyPosition?.TotalScore ?? (int)(Profile?.TotalScore ?? 0);

    /// <summary>Raised whenever any part of the state changes.</summary>
    public event Action? Changed;

    /// <summary>Raised when a new point concerns one of the participant's picks or the participant.</summary>
    public event Action<PointNotification>? PointReceived;

    public async Task<ParticipantSessionStatus> EnsureLoadedAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            if (GameId == gameId && Status == ParticipantSessionStatus.Ready)
            {
                return Status;
            }

            Reset(gameId);
            Status = await LoadCoreAsync(cancellationToken);
            Changed?.Invoke();

            if (Status == ParticipantSessionStatus.Ready)
            {
                await AttachHubAsync();
            }
            return Status;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task LoadElementsAsync(CancellationToken cancellationToken = default)
    {
        if (Elements is not null)
        {
            return;
        }
        Elements = await _http.GetFromJsonAsync<List<AvailableElement>>($"/api/games/{GameId}/elements", cancellationToken) ?? [];
        Changed?.Invoke();
    }

    public async Task LoadRulesAsync(CancellationToken cancellationToken = default)
    {
        if (Rules is not null)
        {
            return;
        }
        Rules = await _http.GetFromJsonAsync<List<GameRule>>($"/api/games/{GameId}/rules", cancellationToken) ?? [];
        Changed?.Invoke();
    }

    public async Task<SaveLineupOutcome> SaveLineupAsync(IReadOnlyList<Guid> pickedElementIds, Guid? captainElementId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = await AuthorizedAsync(HttpMethod.Put, "participant/lineup");
            request.Content = JsonContent.Create(new { PickedElementIds = pickedElementIds, CaptainElementId = captainElementId });
            using var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return SaveLineupOutcome.Rejected;
            }

            await RefreshLineupAsync(cancellationToken);
            return SaveLineupOutcome.Saved;
        }
        catch (HttpRequestException)
        {
            return SaveLineupOutcome.NetworkError;
        }
    }

    public void MarkPointsSeen()
    {
        if (HasUnseenPoints)
        {
            HasUnseenPoints = false;
            Changed?.Invoke();
        }
    }

    /// <summary>Forgets the participant on this device.</summary>
    public async Task LeaveAsync()
    {
        await _auth.ClearTokenAsync();
        Reset(Guid.Empty);
        Changed?.Invoke();
    }

    public void Dispose()
    {
        if (_hubAttached)
        {
            _hub.OnLeaderboardUpdated -= OnLeaderboardUpdated;
            _hub.OnScoreEntryAdded -= OnScoreEntryAdded;
            _hub.OnScoreEntryVoided -= OnScoreEntryVoided;
            _hub.OnGameStateChanged -= OnGameStateChanged;
            _hub.OnReconnected -= OnReconnected;
        }
        _loadLock.Dispose();
    }

    private void Reset(Guid gameId)
    {
        GameId = gameId;
        Status = ParticipantSessionStatus.Loading;
        Game = null;
        Profile = null;
        Lineup = null;
        Leaderboard = null;
        Feed = [];
        Elements = null;
        Rules = null;
        RankChanges = new Dictionary<Guid, int>();
        _previousRanks = [];
        HasUnseenPoints = false;
    }

    private async Task<ParticipantSessionStatus> LoadCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var gameResponse = await _http.GetAsync(new Uri($"/api/games/{GameId}", UriKind.Relative), cancellationToken);
            if (gameResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return ParticipantSessionStatus.NotFound;
            }
            gameResponse.EnsureSuccessStatusCode();
            Game = await gameResponse.Content.ReadFromJsonAsync<GameInfo>(cancellationToken);

            if (string.IsNullOrEmpty(await _auth.GetTokenAsync()))
            {
                return ParticipantSessionStatus.NeedsJoin;
            }

            using var profileRequest = await AuthorizedAsync(HttpMethod.Get, "participant/me");
            using var profileResponse = await _http.SendAsync(profileRequest, cancellationToken);
            if (profileResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
            {
                // The token belongs to another game or is no longer valid.
                await _auth.ClearTokenAsync();
                return ParticipantSessionStatus.NeedsJoin;
            }
            profileResponse.EnsureSuccessStatusCode();
            Profile = await profileResponse.Content.ReadFromJsonAsync<ParticipantProfile>(cancellationToken);

            await Task.WhenAll(
                RefreshLineupAsync(cancellationToken),
                RefreshLeaderboardAsync(cancellationToken),
                RefreshFeedAsync(cancellationToken));

            return ParticipantSessionStatus.Ready;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return ParticipantSessionStatus.Error;
        }
    }

    private async Task RefreshLineupAsync(CancellationToken cancellationToken)
    {
        using var request = await AuthorizedAsync(HttpMethod.Get, "participant/lineup");
        using var response = await _http.SendAsync(request, cancellationToken);
        Lineup = response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ParticipantLineup>(cancellationToken)
            : null;
        Changed?.Invoke();
    }

    private async Task RefreshLeaderboardAsync(CancellationToken cancellationToken)
    {
        using var request = await AuthorizedAsync(HttpMethod.Get, "participant/leaderboard");
        using var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return;
        }

        var leaderboard = await response.Content.ReadFromJsonAsync<LeaderboardResponse>(cancellationToken);
        if (leaderboard is null)
        {
            return;
        }

        var current = AllPositions(leaderboard).ToDictionary(p => p.ParticipantId, p => p.Rank);
        if (_previousRanks.Count > 0)
        {
            RankChanges = current
                .Where(p => _previousRanks.ContainsKey(p.Key))
                .ToDictionary(p => p.Key, p => _previousRanks[p.Key] - p.Value);
        }
        _previousRanks = current;
        Leaderboard = leaderboard;
        Changed?.Invoke();
    }

    private async Task<IReadOnlyList<FeedItem>> RefreshFeedAsync(CancellationToken cancellationToken)
    {
        using var request = await AuthorizedAsync(HttpMethod.Get, "participant/activity");
        using var response = await _http.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            Feed = await response.Content.ReadFromJsonAsync<List<FeedItem>>(cancellationToken) ?? [];
            Changed?.Invoke();
        }
        return Feed;
    }

    private async Task AttachHubAsync()
    {
        if (!_hubAttached)
        {
            _hub.OnLeaderboardUpdated += OnLeaderboardUpdated;
            _hub.OnScoreEntryAdded += OnScoreEntryAdded;
            _hub.OnScoreEntryVoided += OnScoreEntryVoided;
            _hub.OnGameStateChanged += OnGameStateChanged;
            _hub.OnReconnected += OnReconnected;
            _hubAttached = true;
        }
        await _hub.StartAsync(GameId);
    }

    private async void OnLeaderboardUpdated(long version)
    {
        if (Leaderboard is null || version > Leaderboard.Version)
        {
            await RunSafelyAsync(RefreshLeaderboardAsync);
        }
    }

    private async void OnScoreEntryAdded(Guid scoreEntryId)
    {
        await RunSafelyAsync(async cancellationToken =>
        {
            var feed = await RefreshFeedAsync(cancellationToken);
            var item = feed.FirstOrDefault(f => f.Id == scoreEntryId);
            if (item is null || !ConcernsMe(item))
            {
                return;
            }

            HasUnseenPoints = true;
            // The leaderboard broadcast may arrive after the entry: refresh it so the rank is current.
            await RefreshLeaderboardAsync(cancellationToken);
            PointReceived?.Invoke(new PointNotification(item, MyRank));
        });
    }

    private async void OnScoreEntryVoided(Guid scoreEntryId) =>
        await RunSafelyAsync(async cancellationToken => await RefreshFeedAsync(cancellationToken));

    private async void OnGameStateChanged(string state)
    {
        if (Game is not null)
        {
            Game = Game with { State = state };
            Changed?.Invoke();
        }
        await RunSafelyAsync(RefreshLineupAsync);
    }

    private async void OnReconnected()
    {
        await RunSafelyAsync(async cancellationToken =>
        {
            await RefreshLineupAsync(cancellationToken);
            await RefreshLeaderboardAsync(cancellationToken);
            await RefreshFeedAsync(cancellationToken);
        });
    }

    private bool ConcernsMe(FeedItem item)
    {
        if (item.ParticipantId is { } participantId)
        {
            return participantId == Profile?.Id;
        }
        return item.ElementId is { } elementId && Lineup?.PickedElementIds.Contains(elementId) == true;
    }

    private static IEnumerable<LeaderboardEntry> AllPositions(LeaderboardResponse leaderboard) =>
        leaderboard.MyPosition is { } me && leaderboard.TopPositions.All(p => p.ParticipantId != me.ParticipantId)
            ? leaderboard.TopPositions.Append(me)
            : leaderboard.TopPositions;

    private async Task<HttpRequestMessage> AuthorizedAsync(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, new Uri($"/api/games/{GameId}/{path}", UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _auth.GetTokenAsync());
        return request;
    }

    private static async Task RunSafelyAsync(Func<CancellationToken, Task> action)
    {
        try
        {
            await action(CancellationToken.None);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            // Offline or a transient failure: the next hub event or the reconnection refetches.
        }
    }
}
