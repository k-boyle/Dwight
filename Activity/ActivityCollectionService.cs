using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Dwight;

/// <summary>
/// Runs a single activity collection pass for a guild: fans the guild out across every
/// registered <see cref="IActivityCollector"/>, then writes the batch to every registered
/// <see cref="IActivitySink"/>. Shared by the background <see cref="ActivityTrackingService"/>
/// and the manual /activity collect command.
/// </summary>
public class ActivityCollectionService
{
    private readonly IReadOnlyList<IActivityCollector> _collectors;
    private readonly IReadOnlyList<IActivitySink> _sinks;
    private readonly ILogger<ActivityCollectionService> _logger;

    public ActivityCollectionService(IEnumerable<IActivityCollector> collectors, IEnumerable<IActivitySink> sinks, ILogger<ActivityCollectionService> logger)
    {
        _collectors = collectors.ToArray();
        _sinks = sinks.ToArray();
        _logger = logger;
    }

    /// <summary>Collects and persists samples for a single guild, returning the number written.</summary>
    public async Task<int> CollectAsync(GuildSettings settings, DateTimeOffset timestamp, CancellationToken cancellationToken)
    {
        if (settings.ClanTag == null)
        {
            _logger.LogDebug("Clan tag not set for {GuildId}, skipping", settings.GuildId);
            return 0;
        }

        var samples = new List<ActivitySample>();
        foreach (var collector in _collectors)
        {
            try
            {
                var collected = await collector.CollectAsync(settings, timestamp, cancellationToken);
                samples.AddRange(collected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Collector {Collector} failed for guild {GuildId}", collector.GetType().Name, settings.GuildId);
            }
        }

        if (samples.Count == 0)
            return 0;

        foreach (var sink in _sinks)
        {
            try
            {
                await sink.WriteAsync(samples, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sink {Sink} failed to write {Count} samples", sink.GetType().Name, samples.Count);
            }
        }

        return samples.Count;
    }
}
