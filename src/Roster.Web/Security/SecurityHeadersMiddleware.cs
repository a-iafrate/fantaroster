using System.Security.Cryptography;

namespace Roster.Web.Security;

/// <summary>Adds the security headers to every response before anything else writes to it.</summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next, IReadOnlyCollection<string> extraConnectSources)
{
    /// <summary>Where the nonce of the current response is kept, for the document that needs to write it.</summary>
    public const string NonceKey = "Roster.CspNonce";

    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        context.Items[NonceKey] = nonce;

        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.ContentSecurityPolicy = ContentSecurityPolicy.Build(nonce, extraConnectSources);
            headers.XContentTypeOptions = "nosniff";
            // Older browsers that do not know frame-ancestors.
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            return Task.CompletedTask;
        });

        return next(context);
    }
}
