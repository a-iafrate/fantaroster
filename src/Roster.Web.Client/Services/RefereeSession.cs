using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Roster.Web.Client.Services;

/// <summary>
/// State of the referee console for one game: invite token, game, elements, rules, recent points and the
/// offline queue. Points are assigned through HTTP; the hub only tells the console to refresh.
/// </summary>
public sealed class RefereeSession : IDisposable
{
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(10);
    private const int RecentCount = 30;

    private readonly HttpClient _http;
    private readonly OfflineScoreStore _queue;
    private readonly GameHubClient _hub;
    private readonly IJSRuntime _js;
    private readonly TimeProvider _time;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private string? _token;
    private ITimer? _retryTimer;
    private bool _hubAttached;
    private List<RefereeEntry> _entries = [];
    private List<PendingScoreAssignment> _pending = [];

    public RefereeSession(HttpClient http, OfflineScoreStore queue, GameHubClient hub, IJSRuntime js, TimeProvider time)
    {
        _http = http;
        _queue = queue;
        _hub = hub;
        _js = js;
        _time = time;
    }

    public Guid GameId { get; private set; }

    public RefereeSessionStatus Status { get; private set; } = RefereeSessionStatus.Loading;

    public GameInfo? Game { get; private set; }

    public string? RefereeName { get; private set; }

    /// <summary>Elements a point can be assigned to.</summary>
    public IReadOnlyList<AvailableElement> Elements { get; private set; } = [];

    /// <summary>All rules of the game, to label any entry.</summary>
    public IReadOnlyList<GameRule> Rules { get; private set; } = [];

    /// <summary>Rules a referee can apply to an element, biggest bonus first.</summary>
    public IReadOnlyList<GameRule> ElementRules => Rules.Where(r => !r.IsPersonal).OrderByDescending(r => r.Points).ToList();

    public bool IsOnline { get; private set; } = true;

    public int PendingCount => _pending.Count;

    public bool CanAssign => Game?.IsLive == true;

    public event Action? Changed;

    /// <summary>Loads the console. An invite token from the link is stored on this device first.</summary>
    public async Task<RefereeSessionStatus> EnsureLoadedAsync(Guid gameId, string? inviteToken = null, CancellationToken cancellationToken = default)
    {
        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrEmpty(inviteToken))
            {
                await StoreTokenAsync(gameId, inviteToken);
                Status = RefereeSessionStatus.Loading;
            }

            if (GameId == gameId && Status == RefereeSessionStatus.Ready)
            {
                return Status;
            }

            GameId = gameId;
            Status = await LoadCoreAsync(cancellationToken);
            Changed?.Invoke();

            if (Status == RefereeSessionStatus.Ready)
            {
                await AttachHubAsync();
                _retryTimer ??= _time.CreateTimer(_ => _ = RetryAsync(), null, RetryInterval, RetryInterval);
            }
            return Status;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <summary>Recent points: the ones still waiting to be sent first, then the sent and voided ones.</summary>
    public IReadOnlyList<RecentPoint> RecentPoints()
    {
        var pending = _pending
            .Where(p => p.GameId == GameId)
            .Reverse()
            .Select(p => new RecentPoint(null, p.IdempotencyKey, p.ElementId, ElementName(p.ElementId), RuleLabel(p.RuleId), RulePoints(p.RuleId), RecentPointStatus.Pending, null));

        var sent = _entries.Select(e => new RecentPoint(
            e.Id, null, e.ElementId, ElementName(e.ElementId), RuleLabel(e.RuleId), e.PointsSnapshot,
            e.Status == "Voided" ? RecentPointStatus.Voided : RecentPointStatus.Sent, e.CreatedAt));

        return pending.Concat(sent).ToList();
    }

    public string ElementName(Guid? elementId) => Elements.FirstOrDefault(e => e.Id == elementId)?.Name ?? string.Empty;

    public async Task<AssignResult> AssignAsync(Guid elementId, GameRule rule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rule);

        var key = Guid.NewGuid().ToString();
        try
        {
            using var request = Authorized(HttpMethod.Post, $"/api/games/{GameId}/score-entries");
            request.Headers.Add("Idempotency-Key", key);
            request.Content = JsonContent.Create(new { ElementId = elementId, RuleId = rule.Id });
            using var response = await _http.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                SetOnline(true);
                var created = await ReadEntryIdAsync(response, cancellationToken);
                await RefreshEntriesAsync(cancellationToken);
                return new AssignResult(AssignStatus.Sent, created, key);
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await InvalidateTokenAsync();
                return new AssignResult(AssignStatus.NotAuthorized);
            }

            if ((int)response.StatusCode < 500)
            {
                // The server understood and refused (for example the game is not live): queuing would never help.
                await RefreshGameAsync(cancellationToken);
                return new AssignResult(AssignStatus.Rejected);
            }
        }
        catch (Exception ex) when (IsTransient(ex))
        {
            // Falls through to the offline queue.
        }

        SetOnline(false);
        await _queue.EnqueueAsync(new PendingScoreAssignment(GameId, elementId, null, rule.Id, key));
        await ReloadQueueAsync();
        return new AssignResult(AssignStatus.Queued, PendingKey: key);
    }

    /// <summary>Undoes a point: voids it on the server, or drops it from the queue if it was not sent yet.</summary>
    public async Task<bool> UndoAsync(AssignResult result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.EntryId is { } entryId)
        {
            return await VoidAsync(entryId, cancellationToken);
        }

        if (result.PendingKey is { } key && await _queue.RemoveAsync(key))
        {
            await ReloadQueueAsync();
            return true;
        }
        return false;
    }

    public async Task<bool> VoidAsync(Guid entryId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = Authorized(HttpMethod.Post, $"/api/games/{GameId}/score-entries/{entryId}/void");
            request.Content = JsonContent.Create(new { Note = "Undone from the referee console" });
            using var response = await _http.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await InvalidateTokenAsync();
                return false;
            }

            SetOnline(true);
            await RefreshEntriesAsync(cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (IsTransient(ex))
        {
            SetOnline(false);
            return false;
        }
    }

    /// <summary>Sends the points that were assigned offline. Safe to call at any time: the server ignores repeats.</summary>
    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        if (!await _syncLock.WaitAsync(0, cancellationToken))
        {
            return;
        }

        try
        {
            if (_token is null || (await _queue.GetQueueAsync()).Count == 0)
            {
                await ReloadQueueAsync();
                return;
            }

            await _queue.SyncAsync(_token);
            await ReloadQueueAsync();
            await RefreshEntriesAsync(cancellationToken);
            SetOnline(_pending.Count == 0);
        }
        catch (Exception ex) when (IsTransient(ex))
        {
            SetOnline(false);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    /// <summary>Forgets the invite on this device.</summary>
    public async Task LeaveAsync()
    {
        await InvalidateTokenAsync();
    }

    public void Dispose()
    {
        _retryTimer?.Dispose();
        if (_hubAttached)
        {
            _hub.OnScoreEntryAdded -= OnScoreChanged;
            _hub.OnScoreEntryVoided -= OnScoreChanged;
            _hub.OnGameStateChanged -= OnGameStateChanged;
            _hub.OnReconnected -= OnReconnected;
        }
        _loadLock.Dispose();
        _syncLock.Dispose();
    }

    private async Task<RefereeSessionStatus> LoadCoreAsync(CancellationToken cancellationToken)
    {
        _token = await ReadTokenAsync(GameId);
        if (string.IsNullOrEmpty(_token))
        {
            return RefereeSessionStatus.NeedsInvite;
        }

        try
        {
            using var gameResponse = await _http.GetAsync(new Uri($"/api/games/{GameId}", UriKind.Relative), cancellationToken);
            if (gameResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return RefereeSessionStatus.NotFound;
            }
            gameResponse.EnsureSuccessStatusCode();
            Game = await gameResponse.Content.ReadFromJsonAsync<GameInfo>(cancellationToken);

            using var meRequest = Authorized(HttpMethod.Get, $"/api/games/{GameId}/referee/me");
            using var meResponse = await _http.SendAsync(meRequest, cancellationToken);
            if (meResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                await InvalidateTokenAsync();
                return RefereeSessionStatus.NeedsInvite;
            }
            meResponse.EnsureSuccessStatusCode();
            RefereeName = (await meResponse.Content.ReadFromJsonAsync<RefereeInfo>(cancellationToken))?.DisplayName;

            Elements = await _http.GetFromJsonAsync<List<AvailableElement>>($"/api/games/{GameId}/elements", cancellationToken) ?? [];
            Rules = await _http.GetFromJsonAsync<List<GameRule>>($"/api/games/{GameId}/rules", cancellationToken) ?? [];
            await RefreshEntriesAsync(cancellationToken);
            await ReloadQueueAsync();
            SetOnline(true);
            return RefereeSessionStatus.Ready;
        }
        catch (Exception ex) when (IsTransient(ex))
        {
            return RefereeSessionStatus.Error;
        }
    }

    private async Task AttachHubAsync()
    {
        if (!_hubAttached)
        {
            _hub.OnScoreEntryAdded += OnScoreChanged;
            _hub.OnScoreEntryVoided += OnScoreChanged;
            _hub.OnGameStateChanged += OnGameStateChanged;
            _hub.OnReconnected += OnReconnected;
            _hubAttached = true;
        }
        await _hub.StartAsync(GameId);
    }

    private async void OnScoreChanged(Guid entryId) => await RunSafelyAsync(RefreshEntriesAsync);

    private async void OnGameStateChanged(string state)
    {
        if (Game is not null)
        {
            Game = Game with { State = state };
            Changed?.Invoke();
        }
        await RunSafelyAsync(RefreshEntriesAsync);
    }

    private async void OnReconnected()
    {
        await RunSafelyAsync(async ct =>
        {
            await RefreshGameAsync(ct);
            await SyncAsync(ct);
        });
    }

    private async Task RetryAsync()
    {
        if (_pending.Count > 0)
        {
            await SyncAsync();
        }
    }

    private async Task RefreshEntriesAsync(CancellationToken cancellationToken)
    {
        _entries = await _http.GetFromJsonAsync<List<RefereeEntry>>($"/api/games/{GameId}/score-entries?count={RecentCount}", cancellationToken) ?? [];
        Changed?.Invoke();
    }

    private async Task RefreshGameAsync(CancellationToken cancellationToken)
    {
        Game = await _http.GetFromJsonAsync<GameInfo>($"/api/games/{GameId}", cancellationToken) ?? Game;
        Changed?.Invoke();
    }

    private async Task ReloadQueueAsync()
    {
        _pending = (await _queue.GetQueueAsync()).Where(p => p.GameId == GameId).ToList();
        Changed?.Invoke();
    }

    private void SetOnline(bool online)
    {
        if (IsOnline != online)
        {
            IsOnline = online;
            Changed?.Invoke();
        }
    }

    private HttpRequestMessage Authorized(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        return request;
    }

    private static async Task<Guid?> ReadEntryIdAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            return body.TryGetProperty("id", out var id) && id.TryGetGuid(out var guid) ? guid : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private string RuleLabel(Guid ruleId) => Rules.FirstOrDefault(r => r.Id == ruleId)?.Label ?? string.Empty;

    private int RulePoints(Guid ruleId) => Rules.FirstOrDefault(r => r.Id == ruleId)?.Points ?? 0;

    private static string StorageKey(Guid gameId) => $"refereeToken.{gameId}";

    private async Task<string?> ReadTokenAsync(Guid gameId)
    {
        try
        {
            return await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey(gameId));
        }
        catch (JSException)
        {
            return null;
        }
    }

    private async Task StoreTokenAsync(Guid gameId, string token)
    {
        _token = token;
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKey(gameId), token);
        }
        catch (JSException)
        {
            // Storage blocked: the token lasts for this page only.
        }
    }

    private async Task InvalidateTokenAsync()
    {
        _token = null;
        Status = RefereeSessionStatus.NeedsInvite;
        try
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey(GameId));
        }
        catch (JSException)
        {
            // Nothing to clean up.
        }
        Changed?.Invoke();
    }

    private static bool IsTransient(Exception ex) =>
        ex is HttpRequestException or TaskCanceledException or JsonException;

    private static async Task RunSafelyAsync(Func<CancellationToken, Task> action)
    {
        try
        {
            await action(CancellationToken.None);
        }
        catch (Exception ex) when (IsTransient(ex))
        {
            // Offline: the retry timer and the reconnection catch up.
        }
    }
}
