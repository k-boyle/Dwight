using System.Collections.Generic;
using Disqord;

namespace Dwight;

public class GuildSettings
{
    public ulong GuildId { get; init; }

    public string? ClanTag { get; set; }

    [MapCommand(typeof(IInteractionChannel))]
    public ulong WelcomeChannelId { get; set; }

    [MapCommand(typeof(IRole))]
    public ulong VerifiedRoleId { get; set; }

    [MapCommand(typeof(IRole))]
    public ulong UnverifiedRoleId { get; set; }

    [MapCommand(typeof(IInteractionChannel))]
    public ulong WarChannelId { get; set; }

    [MapCommand(typeof(IInteractionChannel))]
    public ulong GeneralChannelId { get; set; }
    
    [MapCommand(typeof(IRole))]
    public ulong ElderRoleId { get; set; }

    [MapCommand(typeof(IRole))]
    public ulong CoLeaderRoleId { get; set; }
    
    [MapCommand(typeof(IRole))]
    public ulong WarRoleId { get; set; }

    public List<ClashMember> Members { get; set; } = new();
    
    public CurrentWarReminder? CurrentWarReminder { get; set; }

    public GuildSettings(
        ulong guildId,
        string clanTag,
        ulong welcomeChannelId,
        ulong verifiedRoleId,
        ulong unverifiedRoleId,
        ulong warChannelId,
        ulong generalChannelId,
        ulong elderRoleId,
        ulong coLeaderRoleId,
        CurrentWarReminder currentWarReminder)
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
        CurrentWarReminder = currentWarReminder;
    }

    public GuildSettings(ulong guildId)
    {
        GuildId = guildId;
    }
}