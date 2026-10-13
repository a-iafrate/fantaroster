using System.Diagnostics.CodeAnalysis;

namespace Roster.Infrastructure.Payments;

/// <summary>
/// The access token of PayPal, shared by every request of the application (the gateway itself is created per request, and
/// asking for a token each time would be slow and would be limited by PayPal).
/// </summary>
public sealed class PayPalTokenCache : IDisposable
{
    private readonly object _gate = new();
    private string? _token;
    private DateTimeOffset _expires;

    public SemaphoreSlim Lock { get; } = new(1, 1);

    public bool TryGet(DateTimeOffset now, [NotNullWhen(true)] out string? token)
    {
        lock (_gate)
        {
            token = now < _expires ? _token : null;
            return token is not null;
        }
    }

    public void Store(string token, DateTimeOffset expires)
    {
        lock (_gate)
        {
            _token = token;
            _expires = expires;
        }
    }

    public void Dispose() => Lock.Dispose();
}
