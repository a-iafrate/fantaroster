using Microsoft.Extensions.DependencyInjection;
using Roster.Application.Ports;
using Roster.Infrastructure.Data.Repositories;
using Roster.Infrastructure.Plugins;

namespace Roster.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IElementRepository, ElementRepository>();
        services.AddScoped<IParticipantRepository, ParticipantRepository>();
        services.AddScoped<IScoreEntryRepository, ScoreEntryRepository>();
        
        services.AddSingleton<IPluginRegistry, PluginRegistry>();

        return services;
    }
}
