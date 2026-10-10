using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Roster.Web.Client.Services;

public sealed record PendingScoreAssignment(
    Guid GameId,
    Guid? ElementId,
    Guid? ParticipantId,
    Guid RuleId,
    string IdempotencyKey
);

public class OfflineScoreStore
{
    private readonly IJSRuntime _jsRuntime;
    private readonly HttpClient _httpClient;
    private const string StorageKey = "OfflineScoreStore";

    public OfflineScoreStore(IJSRuntime jsRuntime, HttpClient httpClient)
    {
        _jsRuntime = jsRuntime;
        _httpClient = httpClient;
    }

    public async Task EnqueueAsync(PendingScoreAssignment assignment)
    {
        var queue = await GetQueueAsync();
        queue.Add(assignment);
        await SaveQueueAsync(queue);
    }

    public async Task<List<PendingScoreAssignment>> GetQueueAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", StorageKey);
            if (!string.IsNullOrEmpty(json))
            {
                return JsonSerializer.Deserialize<List<PendingScoreAssignment>>(json) ?? new List<PendingScoreAssignment>();
            }
        }
        catch
        {
            // Ignore
        }
        return new List<PendingScoreAssignment>();
    }

    private async Task SaveQueueAsync(List<PendingScoreAssignment> queue)
    {
        try
        {
            var json = JsonSerializer.Serialize(queue);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        }
        catch
        {
            // Ignore
        }
    }

    public async Task SyncAsync(string refereeToken)
    {
        var queue = await GetQueueAsync();
        if (queue.Count == 0) return;

        var remaining = new List<PendingScoreAssignment>();

        foreach (var assignment in queue)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"/api/games/{assignment.GameId}/score-entries");
                request.Headers.Add("Idempotency-Key", assignment.IdempotencyKey);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", refereeToken);

                request.Content = JsonContent.Create(new
                {
                    ElementId = assignment.ElementId,
                    ParticipantId = assignment.ParticipantId,
                    RuleId = assignment.RuleId
                });

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    // If conflict/bad request, we might want to drop it, but if network error, keep it.
                    // Assuming 5xx or network exceptions will throw or return false.
                    // For simplicity, let's keep it if it's 5xx, otherwise drop.
                    var statusCode = (int)response.StatusCode;
                    if (statusCode >= 500)
                    {
                        remaining.Add(assignment);
                    }
                }
            }
            catch
            {
                // Network error
                remaining.Add(assignment);
            }
        }

        await SaveQueueAsync(remaining);
    }
}
