namespace Dwight;

/// <summary>
/// Tracks a player's lifetime troops donated. An increase between samples means they donated.
/// </summary>
public class DonationsMetric : IPlayerMetric
{
    public string Key => "donations";

    public int Extract(Player player) => player.Donations;
}
