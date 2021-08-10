namespace Dwight
{
    public class FwaRep
    {
        public ulong GuildId { get; init; }
        public ulong DiscordId { get; init; }
        public float TimeZone { get; set; }
        public ClashMember Member { get; set; }

        public FwaRep(ulong guildId, ulong discordId, float timeZone)
        {
            GuildId = guildId;
            DiscordId = discordId;
            TimeZone = timeZone;
        }

        public FwaRep(ulong guildId, ulong discordId)
        {
            GuildId = guildId;
            DiscordId = discordId;
        }
    }
}