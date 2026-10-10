using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Roster.Application.Games.Queries;

public sealed class LeaderboardStateStore
{
    private readonly ConcurrentDictionary<Guid, GameLeaderboardState> _states = new();

    public GameLeaderboardState GetOrAddState(Guid gameId)
    {
        return _states.GetOrAdd(gameId, _ => new GameLeaderboardState());
    }
}

public sealed class GameLeaderboardState
{
    private long _version;
    private List<LeaderboardDto>? _leaderboard;

    public long Version => _version;

    public void Update(List<LeaderboardDto> leaderboard)
    {
        _leaderboard = leaderboard;
        _version++;
    }

    public List<LeaderboardDto>? GetLeaderboard() => _leaderboard;
}
