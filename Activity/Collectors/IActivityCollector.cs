using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dwight;

/// <summary>
/// Produces activity samples for a guild's clan from a single data source (e.g. the players
/// endpoint, the current war). One collector per API resource so each resource is fetched
/// once per cycle. The data-source level extension point.
/// </summary>
public interface IActivityCollector
{
    Task<IReadOnlyCollection<ActivitySample>> CollectAsync(GuildSettings settings, DateTimeOffset timestamp, CancellationToken cancellationToken);
}
