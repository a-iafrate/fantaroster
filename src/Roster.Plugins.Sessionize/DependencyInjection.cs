using Microsoft.Extensions.DependencyInjection;
using Roster.Plugins.Abstractions;

namespace Roster.Plugins.Sessionize;

public static class DependencyInjection
{
    public static IServiceCollection AddSessionizePlugin(this IServiceCollection services)
    {
        services.AddHttpClient<IElementSourcePlugin, SessionizeElementSourcePlugin>()
                .AddStandardResilienceHandler();

        return services;
    }
}
