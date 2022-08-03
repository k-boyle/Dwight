using System.Collections.Generic;
using Disqord;

namespace Dwight;

public class GuildSettings
{
    public ulong GuildId { get; init; }

    public string? ClanTag { get; set; }

    [MapCommand(typeof(ITextChannel))]
    public ulong WelcomeChannelId { get; set; }

    [MapCommand(typeof(IRole))]
    public ulong VerifiedRoleId { get; set; }

    [MapCommand(typeof(IRole))]
    public ulong UnverifiedRoleId { get; set; }

    [MapCommand(typeof(ITextChannel))]
    public ulong WarChannelId { get; set; }

    [MapCommand(typeof(ITextChannel))]
    public ulong GeneralChannelId { get; set; }
    
    [MapCommand(typeof(IRole))]
    public ulong ElderRoleId { get; set; }

    [MapCommand(typeof(IRole))]
    public ulong CoLeaderRoleId { get; set; }
    
    [MapCommand(typeof(IRole))]
    public ulong WarRole { get; set; }

    public List<ClashMember> Members { get; set; } = new();

    public GuildSettings(
        ulong guildId,
        string clanTag,
        ulong welcomeChannelId,
        ulong verifiedRoleId,
        ulong unverifiedRoleId,
        ulong warChannelId,
        ulong generalChannelId,
        ulong elderRoleId,
        ulong coLeaderRoleId)
    {
        GuildId = guildId;
        ClanTag = clanTag;
        WelcomeChannelId = welcomeChannelId;
        VerifiedRoleId = verifiedRoleId;
        UnverifiedRoleId = unverifiedRoleId;
        WarChannelId = warChannelId;
        GeneralChannelId = generalChannelId;
        ElderRoleId = elderRoleId;
        CoLeaderRoleId = coLeaderRoleId;
    }

    public GuildSettings(ulong guildId)
    {
        GuildId = guildId;
    }
}