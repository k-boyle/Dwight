namespace Dwight
{
    public class ClashMember
    {
        public ulong GuildId { get; init; }
        public ulong DiscordId { get; init; }
        public string[] Tags { get; set; }
        public int MainTag { get; set; }

        public ClashMember(ulong guildId, ulong discordId, string[] tags, int mainTag)
        {
            GuildId = guildId;
            DiscordId = discordId;
            Tags = tags;
            MainTag = mainTag;
        }

        public ClashMember(ulong guildId, ulong discordId)
        {
            GuildId = guildId;
            DiscordId = discordId;
        }
    }
}