namespace Dwight;

public static class GuildSettingsExtensions
{
    /// <summary>
    /// Returns whether this guild has a clan tag that is at least shaped like a real Clash
    /// tag (starts with '#'), guarding against blank or garbage values that were saved
    /// without validation (e.g. someone setting the tag to a literal "" or " ").
    /// </summary>
    public static bool TryGetClanTag(this GuildSettings settings, out string clanTag)
    {
        if (settings.ClanTag is { Length: > 1 } tag && tag[0] == '#')
        {
            clanTag = tag;
            return true;
        }

        clanTag = "";
        return false;
    }
}
