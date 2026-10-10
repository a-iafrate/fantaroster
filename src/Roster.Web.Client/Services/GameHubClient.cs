using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Roster.Web.Client.Services;

public class GameHubClient : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly NavigationManager _navigationManager;
    private readonly ILogger<GameHubClient> _logger;

    public event Action<long>? OnLeaderboardUpdated;
    public event Action<Guid>? OnScoreEntryAdded;
    public event Action<Guid>? OnScoreEntryVoided;
    public event Action<string>? OnGameStateChanged;

    /// <summary>Raised after an automatic reconnection: events may have been missed, so listeners refetch.</summary>
    public event Action? OnReconnected;

    public GameHubClient(NavigationManager navigationManager, ILogger<GameHubClient> logger)
    {
        _navigationManager = navigationManager;
        _logger = logger;
    }

    public async Task StartAsync(Guid gameId)
    {
        if (_hubConnection is not null) return;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("/hubs/game"))
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<long>("LeaderboardUpdated", version =>
        {
            OnLeaderboardUpdated?.Invoke(version);
        });

        _hubConnection.On<Guid>("ScoreEntryAdded", scoreEntryId =>
        {
            OnScoreEntryAdded?.Invoke(scoreEntryId);
        });

        _hubConnection.On<Guid>("ScoreEntryVoided", scoreEntryId =>
        {
            OnScoreEntryVoided?.Invoke(scoreEntryId);
        });

        _hubConnection.On<string>("GameStateChanged", state =>
        {
            OnGameStateChanged?.Invoke(state);
        });

        // Group membership does not survive a reconnection: join the game group again.
        _hubConnection.Reconnected += async _ =>
        {
            await _hubConnection.InvokeAsync("JoinGame", gameId.ToString());
            OnReconnected?.Invoke();
        };

        try
        {
            await _hubConnection.StartAsync();
            await _hubConnection.InvokeAsync("JoinGame", gameId.ToString());
        }
        catch (Exception ex)
        {
#pragma warning disable CA1848 // Justification: one-off client error log.
            _logger.LogError(ex, "Failed to start GameHub connection.");
#pragma warning restore CA1848
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }
}
