using Microsoft.AspNetCore.Localization;

namespace Roster.Web.Brand;

public static class CultureEndpoints
{
    public static readonly string[] SupportedCultures = ["en", "it"];

    public static IEndpointRouteBuilder MapCultureEndpoints(this IEndpointRouteBuilder app)
    {
        // A plain link (it also works in static pages): remember the language in a cookie, then go back.
        app.MapGet("/culture/set", (string culture, string? returnUrl, HttpContext context) =>
        {
            if (!SupportedCultures.Contains(culture, StringComparer.OrdinalIgnoreCase))
            {
                return Results.BadRequest();
            }

            context.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture.ToLowerInvariant())),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    Secure = context.Request.IsHttps,
                });

            return Results.LocalRedirect(IsLocal(returnUrl) ? returnUrl! : "/");
        })
        .AllowAnonymous()
        .ExcludeFromDescription();

        return app;
    }

    /// <summary>Only paths on this site: never "//other.example" or "/\other.example", which browsers read as other hosts.</summary>
    public static bool IsLocal(string? url) =>
        !string.IsNullOrEmpty(url)
        && url[0] == '/'
        && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));
}
