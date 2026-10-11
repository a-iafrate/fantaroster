using Microsoft.AspNetCore.Components;
using Roster.Web.Options;

namespace Roster.Web.Brand;

/// <summary>
/// Scoped per request or per circuit. During a request the brand comes from <see cref="BrandMiddleware"/>;
/// inside an interactive circuit there is no request any more, so the host of the page is used.
/// </summary>
public sealed class BrandAccessor(BrandResolver resolver, IHttpContextAccessor httpContextAccessor, IServiceProvider services) : IBrandAccessor
{
    private BrandDefinition? _current;

    public BrandDefinition Current => _current ??= ResolveCurrent();

    public BrandDefinition ForGame(string? brandId) => resolver.GetById(brandId);

    private BrandDefinition ResolveCurrent()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is not null)
        {
            return context.Items[BrandMiddleware.ItemKey] as BrandDefinition ?? resolver.Resolve(context.Request.Host.Host);
        }

        try
        {
            var navigation = services.GetService<NavigationManager>();
            if (navigation is not null && Uri.TryCreate(navigation.BaseUri, UriKind.Absolute, out var uri))
            {
                return resolver.Resolve(uri.Host);
            }
        }
        catch (InvalidOperationException)
        {
            // The navigation manager is not initialized yet (very early in a circuit): fall back below.
        }
        return resolver.Default;
    }
}
