using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dwight;

/// <summary>
/// Default sink: appends samples to the activity_samples table. This table is the canonical
/// store any representation tool (Grafana, LookerStudio, BigQuery export) reads from.
/// </summary>
public class PostgresActivitySink : IActivitySink
{
    private readonly DwightDbContext _dbContext;

    public PostgresActivitySink(DwightDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task WriteAsync(IReadOnlyCollection<ActivitySample> samples, CancellationToken cancellationToken)
    {
        if (samples.Count == 0)
            return;

        await _dbContext.ActivitySamples.AddRangeAsync(samples, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
