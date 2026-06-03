using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dwight;

/// <summary>
/// Receives a batch of collected samples and persists/exports them. The seam that decouples
/// collection from the data representation layer: implement this to send samples to Postgres,
/// BigQuery, Grafana, etc. Multiple sinks may be registered and are all written to.
/// </summary>
public interface IActivitySink
{
    Task WriteAsync(IReadOnlyCollection<ActivitySample> samples, CancellationToken cancellationToken);
}
