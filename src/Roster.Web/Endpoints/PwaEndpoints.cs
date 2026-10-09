using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Roster.Web.Options;

namespace Roster.Web.Endpoints;

public static class PwaEndpoints
{
    public static void MapPwaEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/manifest.webmanifest", (IOptions<BrandOptions> options) =>
        {
            var brand = options.Value;
            var isFanta = brand.Id == "fantaroster";

            return Results.Json(new
            {
                name = brand.Name,
                short_name = brand.Name,
                start_url = "/",
                display = "standalone",
                background_color = isFanta ? "#1A1B1E" : "#FFFFFF",
                theme_color = isFanta ? "#F59E0B" : "#2563EB",
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
