using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Dwight;

/// <summary>
/// Collects per-player stats from the /players endpoint. Fetches every tracked tag in the
/// clan as a concurrent burst, then runs each registered <see cref="IPlayerMetric"/> over
/// the result. Add a metric to track a new stat; no changes needed here.
/// </summary>
public class PlayerStatsCollector : IActivityCollector
{
    private readonly ClashApiClient _clashApiClient;
    private readonly IReadOnlyList<IPlayerMetric> _metrics;
    private readonly ILogger<PlayerStatsCollector> _logger;

    public PlayerStatsCollector(ClashApiClient clashApiClient, IEnumerable<IPlayerMetric> metrics, ILogger<PlayerStatsCollector> logger)
    {
        _clashApiClient = clashApiClient;
        _metrics = metrics.ToArray();
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<ActivitySample>> CollectAsync(GuildSettings settings, DateTimeOffset timestamp, CancellationToken cancellationToken)
    {
        if (settings.ClanTag is not { } clanTag || _metrics.Count == 0)
            return Array.Empty<ActivitySample>();

        var tags = settings.Members.SelectMany(member => member.Tags).Distinct().ToArray();
        if (tags.Length == 0)
            return Array.Empty<ActivitySample>();

        var players = await Task.WhenAll(tags.Select(tag => FetchAsync(tag, cancellationToken)));

        var samples = new List<ActivitySample>(players.Length * _metrics.Count);
        foreach (var (tag, player) in players)
        {
            if (player == null)
                continue;

            foreach (var metric in _metrics)
                samples.Add(new(timestamp, settings.GuildId, clanTag, tag, metric.Key, metric.Extract(player)));
        }

        return samples;
    }

    private async Task<(string Tag, Player? Player)> FetchAsync(string tag, CancellationToken cancellationToken)
    {
        // One retry with a short backoff so a transient failure (e.g. throttling) doesn't
        // drop this player's sample for the whole cycle.
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return (tag, await _clashApiClient.GetPlayerAsync(tag, cancellationToken));
            }
            catch (Exception ex) when (attempt == 0 && ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to fetch player {Tag}, retrying once", tag);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch player {Tag}, skipping for this cycle", tag);
                return (tag, null);
            }
        }
    }
}
