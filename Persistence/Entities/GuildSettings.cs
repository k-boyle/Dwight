using System.Collections.Generic;
using Disqord;

namespace Dwight
{
    public class GuildSettings
    {
        public ulong GuildId { get; init; }
        
        public string ClanTag { get; set; }
        
        [MapCommand(typeof(ITextChannel))]
        public ulong WelcomeChannelId { get; set; }
        
        [MapCommand(typeof(IRole))]
        public ulong VerifiedRoleId { get; set; }
        
        [MapCommand(typeof(IRole))]
        public ulong UnverifiedRoleId { get; set; }
        
        [MapCommand(typeof(ITextChannel))]
        public ulong WarChannelId { get; set; }
        
        [MapCommand(typeof(ITextChannel))]
        public ulong StartTimeChannelId { get; set; }
        
        [MapCommand(typeof(ITextChannel))]
        public ulong GeneralChannelId { get; set; }
        
        public string CalendarLink { get; set; }
        
        [MapCommand(typeof(IRole))]
        public ulong ElderRoleId { get; set; }
        
        [MapCommand(typeof(IRole))]
        public ulong CoLeaderRoleId { get; set; }
        
        [MapCommand(typeof(IRole))]
        public ulong RepRoleId { get; set; }
        
        public List<ClashMember> Members { get; set; } = new();
        public List<FwaRep> FwaReps { get; set; } = new();

        public GuildSettings(
            ulong guildId,
            string clanTag,
            ulong welcomeChannelId,
            ulong verifiedRoleId,
            ulong unverifiedRoleId,
            ulong warChannelId,
            ulong startTimeChannelId,
            ulong generalChannelId,
            string calendarLink,
            ulong elderRoleId,
            ulong coLeaderRoleId,
            ulong repRoleId)
        {
            GuildId = guildId;
            ClanTag = clanTag;
            WelcomeChannelId = welcomeChannelId;
            VerifiedRoleId = verifiedRoleId;
            UnverifiedRoleId = unverifiedRoleId;
            WarChannelId = warChannelId;
            StartTimeChannelId = startTimeChannelId;
            GeneralChannelId = generalChannelId;
            CalendarLink = calendarLink;
            ElderRoleId = elderRoleId;
            CoLeaderRoleId = coLeaderRoleId;
            RepRoleId = repRoleId;
        }
        
        public GuildSettings(ulong guildId)
        {
            GuildId = guildId;
        }
    }
}