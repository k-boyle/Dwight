using Microsoft.Extensions.DependencyInjection;

namespace Dwight;

public static class ActivityServiceExtensions
{
    /// <summary>
    /// Registers the activity tracking collectors, metrics and sinks. Extend by adding more
    /// <see cref="IPlayerMetric"/>, <see cref="IActivityCollector"/> or <see cref="IActivitySink"/>
    /// registrations here. The <see cref="ActivityTrackingService"/> itself is auto-discovered.
    /// </summary>
    public static IServiceCollection AddActivityTracking(this IServiceCollection collection)
    {
        collection.AddScoped<IPlayerMetric, DonationsMetric>();
        collection.AddScoped<IPlayerMetric, DonationsReceivedMetric>();
        collection.AddScoped<IPlayerMetric, ClanCapitalContributionsMetric>();
        collection.AddScoped<IActivityCollector, PlayerStatsCollector>();
        collection.AddScoped<IActivitySink, PostgresActivitySink>();
        collection.AddScoped<ActivityCollectionService>();

        return collection;
    }
}
