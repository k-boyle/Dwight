namespace Dwight;

/// <summary>
/// Tracks a player's lifetime attack wins. Used as a proxy for "is this player actually
/// playing the game" — an increase between samples means they attacked.
/// </summary>
public class AttackWinsMetric : IPlayerMetric
{
    public string Key => "attack_wins";

    public int Extract(Player player) => player.AttackWins;
}
