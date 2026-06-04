using System.Linq;

namespace Dwight;

/// <summary>
/// Tracks a player's lifetime multiplayer battles won, sourced from the "Conqueror" achievement
/// ("Win 5000 Multiplayer battles"). The dedicated attackWins field stopped being returned
/// reliably, but this cumulative achievement value still climbs with every attack won, so an
/// increase between samples means the player has been attacking.
/// </summary>
public class MultiplayerBattlesWonMetric : IPlayerMetric
{
    private const string ConquerorAchievement = "Conqueror";

    public string Key => "multiplayer_battles_won";

    public int Extract(Player player)
        => player.Achievements?.FirstOrDefault(achievement => achievement.Name == ConquerorAchievement)?.Value ?? 0;
}
