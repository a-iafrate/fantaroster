using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Roster.Web.Hubs;

public sealed class GameHub : Hub
{
    public async Task JoinGame(string gameId)
    {
        if (Guid.TryParse(gameId, out var id))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"game:{id}");
        }
    }

    public async Task LeaveGame(string gameId)
    {
        if (Guid.TryParse(gameId, out var id))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"game:{id}");
        }
    }
}
