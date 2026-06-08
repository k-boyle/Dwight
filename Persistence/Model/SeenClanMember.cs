namespace Dwight;

public class SeenClanMember
{
    public ulong GuildId { get; init; }
    public string Tag { get; init; }

    public SeenClanMember(ulong guildId, string tag)
    {
        GuildId = guildId;
        Tag = tag;
    }
}
