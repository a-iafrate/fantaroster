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

public sealed partial class ResyncBackgroundJob : BackgroundService
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
                LogJobExecutionError(_logger, ex);
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
                LogResyncStarted(_logger, binding.GameId, binding.Id);
                var result = await resyncService.ResyncGameElementsAsync(binding.GameId, binding.Id, cancellationToken);
                LogResyncFinished(_logger, result.AddedCount, result.UpdatedCount, result.MarkedMissingCount);
            }
            catch (Exception ex)
            {
                LogResyncFailed(_logger, binding.Id, ex);
            }
        }
    }

    [LoggerMessage(LogLevel.Error, "Error occurred executing resync background job.")]
    private static partial void LogJobExecutionError(ILogger logger, Exception ex);

    [LoggerMessage(LogLevel.Information, "Starting background resync for Game {GameId}, Binding {BindingId}")]
    private static partial void LogResyncStarted(ILogger logger, Guid gameId, Guid bindingId);

    [LoggerMessage(LogLevel.Information, "Resync finished. Added: {Added}, Updated: {Updated}, Missing: {Missing}")]
    private static partial void LogResyncFinished(ILogger logger, int added, int updated, int missing);

    [LoggerMessage(LogLevel.Error, "Failed to resync binding {BindingId}")]
    private static partial void LogResyncFailed(ILogger logger, Guid bindingId, Exception ex);
}
