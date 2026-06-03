using System.Linq;

namespace Dwight;

/// <summary>
/// Tracks whether a player currently carries the in-game "Active Daily" label. Unlike the
/// counter metrics this is a state, recorded as 1 (has the label) or 0 (does not), so downstream
/// reads it as the player's status at sample time rather than as a delta.
/// </summary>
public class ActiveDailyLabelMetric : IPlayerMetric
{
    // Stable label id from the players API; the display name can localise, the id does not.
    private const long ActiveDailyLabelId = 57000009;

    public string Key => "active_daily_label";

    public int Extract(Player player)
        => player.Labels?.Any(label => label.Id == ActiveDailyLabelId) == true ? 1 : 0;
}
