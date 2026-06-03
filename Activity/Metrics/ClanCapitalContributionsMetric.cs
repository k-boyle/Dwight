namespace Dwight;

/// <summary>
/// Tracks a player's lifetime clan capital gold contributed. An increase between samples means
/// they contributed to the clan capital (a raid weekend / capital activity signal).
/// </summary>
public class ClanCapitalContributionsMetric : IPlayerMetric
{
    public string Key => "clan_capital_contributions";

    public int Extract(Player player) => player.ClanCapitalContributions;
}
