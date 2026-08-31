namespace Dwight;

public class TownhallBase
{
    public ulong GuildId { get; init; }
    public int Level { get; init; }
    public string Link { get; set; }

    public TownhallBase(ulong guildId, int level, string link)
    {
        GuildId = guildId;
        Level = level;
        Link = link;
    }
}
