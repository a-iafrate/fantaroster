using Microsoft.JSInterop;

namespace Roster.Web.Client.Services;

public class ParticipantAuthState
{
    private readonly IJSRuntime _jsRuntime;
    private string? _token;

    public ParticipantAuthState(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string?> GetTokenAsync()
    {
        if (_token is not null)
            return _token;

        try
        {
            _token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "participantToken");
        }
        catch
        {
            // Ignore JS interop errors if running in SSR or unsupported environment
        }

        return _token;
    }

    public async Task SetTokenAsync(string token)
    {
        _token = token;
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "participantToken", token);
        }
        catch
        {
            // Ignore
        }
    }

    public async Task ClearTokenAsync()
    {
        _token = null;
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "participantToken");
        }
        catch
        {
            // Ignore
        }
    }
}
