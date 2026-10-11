using Microsoft.AspNetCore.Localization;
using Roster.Web.Options;

namespace Roster.Web.Brand;

/// <summary>The language of the brand, used until the person picks one (the cookie provider comes first).</summary>
public sealed class BrandCultureProvider(BrandResolver resolver) : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var brand = httpContext.Items[BrandMiddleware.ItemKey] as BrandDefinition ?? resolver.Resolve(httpContext.Request.Host.Host);
        return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(brand.DefaultCulture));
    }
}
