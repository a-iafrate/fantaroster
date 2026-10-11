namespace Roster.Web.Brand;

/// <summary>Resolves the brand from the host once per request and makes it available to everything after it.</summary>
public sealed class BrandMiddleware(RequestDelegate next, BrandResolver resolver)
{
    public const string ItemKey = "Roster.Brand";

    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Items[ItemKey] = resolver.Resolve(context.Request.Host.Host);
        // The same URL answers differently per host (brand, language, manifest): caches must keep them apart.
        context.Response.Headers.Append("Vary", "Host");
        return next(context);
    }
}
