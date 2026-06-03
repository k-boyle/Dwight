using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disqord.Bot.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dwight;

/// <summary>
/// Polls every registered <see cref="IActivityCollector"/> for each guild's clan, batches the
/// resulting samples and hands them to every registered <see cref="IActivitySink"/>. Periodically
/// prunes samples older than the configured retention period. Collection is fully decoupled from
/// representation — sinks are the only thing aware of where data ends up.
/// </summary>
public class ActivityTrackingService : DiscordBotService
{
    private readonly PollingConfiguration _pollingConfiguration;
    private DateTimeOffset _lastRetention = DateTimeOffset.MinValue;

    public ActivityTrackingService(IOptions<PollingConfiguration> pollingConfiguration)
    {
        _pollingConfiguration = pollingConfiguration.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_pollingConfiguration.ActivityTrackingEnabled)
            return;

        await Bot.WaitUntilReadyAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TrackActivityAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An exception occured whilst tracking activity");
            }

            await Task.Delay(_pollingConfiguration.ActivityTrackingPollingDuration, stoppingToken);
        }
    }

    private async Task TrackActivityAsync(CancellationToken cancellationToken)
    {
        var timestamp = DateTimeOffset.UtcNow;

        await using var scope = Bot.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetDwightDbContext();
        var collectionService = scope.ServiceProvider.GetRequiredService<ActivityCollectionService>();

        var allSettings = await context.GuildSettings
            .ToListAsync(cancellationToken);

        var total = 0;
        foreach (var settings in allSettings)
            total += await collectionService.CollectAsync(settings, timestamp, cancellationToken);

        if (total > 0)
            Logger.LogInformation("Collected {Count} activity samples", total);

        await ApplyRetentionAsync(context, timestamp, cancellationToken);
    }

    private async Task ApplyRetentionAsync(DwightDbContext context, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var retention = _pollingConfiguration.ActivityRetentionPeriod;
        if (retention <= TimeSpan.Zero)
            return;

        // Cheap to run, but no need to do it every minute.
        if (now - _lastRetention < TimeSpan.FromHours(1))
            return;

        _lastRetention = now;
        var cutoff = now - retention;
        var deleted = await context.ActivitySamples
            .Where(sample => sample.Timestamp < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0)
            Logger.LogInformation("Pruned {Count} activity samples older than {Cutoff}", deleted, cutoff);
    }
}
