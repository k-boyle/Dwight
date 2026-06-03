namespace Dwight;

/// <summary>
/// Tracks a player's lifetime troops received. An increase between samples means they
/// requested and received donations.
/// </summary>
public class DonationsReceivedMetric : IPlayerMetric
{
    public string Key => "donations_received";

    public int Extract(Player player) => player.DonationsReceived;
}
