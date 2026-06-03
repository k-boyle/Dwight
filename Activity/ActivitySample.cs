using System;

namespace Dwight;

/// <summary>
/// A single, immutable observation of a metric for a player at a point in time.
/// This is the canonical, representation-agnostic record produced by collectors and
/// handed to sinks. Values are raw (e.g. lifetime counters); deltas are derived downstream.
/// </summary>
public class ActivitySample
{
    public long Id { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public ulong GuildId { get; init; }
    public string ClanTag { get; init; }
    public string PlayerTag { get; init; }
    public string MetricKey { get; init; }
    public int Value { get; init; }

    public ActivitySample(DateTimeOffset timestamp, ulong guildId, string clanTag, string playerTag, string metricKey, int value)
    {
        Timestamp = timestamp;
        GuildId = guildId;
        ClanTag = clanTag;
        PlayerTag = playerTag;
        MetricKey = metricKey;
        Value = value;
    }
}
