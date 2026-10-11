using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Roster.Web.Brand;

namespace Roster.Web.Endpoints;

public static class PwaEndpoints
{
    public static void MapPwaEndpoints(this IEndpointRouteBuilder app)
    {
        // One manifest URL, a different manifest per brand (the brand comes from the host).
        app.MapGet("/manifest.webmanifest", (IBrandAccessor brands) =>
        {
            var brand = brands.Current;

            return Results.Json(new
            {
                id = "/",
                name = brand.Name,
                short_name = brand.Name,
                lang = brand.DefaultCulture,
                start_url = "/",
                display = "standalone",
                background_color = brand.BackgroundColor,
                theme_color = brand.ThemeColor,
                icons = new[]
                {
                    new
                    {
                        src = "/favicon.png",
                        sizes = "192x192 512x512",
                        type = "image/png"
                    }
                }
            });
        });
    }
}
