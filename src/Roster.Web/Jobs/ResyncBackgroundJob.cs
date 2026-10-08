using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Roster.Application.Ports;
using Roster.Infrastructure.Data;

namespace Roster.Web.Jobs;

public sealed class ResyncBackgroundJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ResyncBackgroundJob> _logger;

    public ResyncBackgroundJob(IServiceProvider serviceProvider, ILogger<ResyncBackgroundJob> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoWorkAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing resync background job.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RosterDbContext>();
        var resyncService = scope.ServiceProvider.GetRequiredService<IResyncService>();

        var cutoff = DateTimeOffset.UtcNow.AddHours(-1);

        var bindingsToSync = await dbContext.SourceBindings
            .Where(sb => sb.LastSyncTime == null || sb.LastSyncTime < cutoff)
            .ToListAsync(cancellationToken);

        foreach (var binding in bindingsToSync)
        {
            try
            {
                _logger.LogInformation("Starting background resync for Game {GameId}, Binding {BindingId}", binding.GameId, binding.Id);
                var result = await resyncService.ResyncGameElementsAsync(binding.GameId, binding.Id, cancellationToken);
                _logger.LogInformation("Resync finished. Added: {Added}, Updated: {Updated}, Missing: {Missing}", result.AddedCount, result.UpdatedCount, result.MarkedMissingCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resync binding {BindingId}", binding.Id);
            }
        }
    }
}
