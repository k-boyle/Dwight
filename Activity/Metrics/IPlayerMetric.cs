namespace Dwight;

/// <summary>
/// A field-level metric extracted from a <see cref="Player"/>. The cheapest extension point:
/// add an implementation to track a new player stat without any additional API calls.
/// </summary>
public interface IPlayerMetric
{
    /// <summary>Stable identifier persisted with every sample, e.g. "donations".</summary>
    string Key { get; }

    int Extract(Player player);
}
