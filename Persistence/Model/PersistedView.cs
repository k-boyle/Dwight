namespace Dwight;

public enum PersistedViewType
{
    Welcome,
    VerificationCompleted
}

public class PersistedView
{
    public ulong MessageId { get; init; }
    public ulong ChannelId { get; init; }
    public ulong GuildId { get; init; }
    public PersistedViewType Type { get; init; }
    public ulong UserId { get; init; }
    public string? Tag { get; init; }

    public PersistedView(ulong messageId, ulong channelId, ulong guildId, PersistedViewType type, ulong userId, string? tag)
    {
        MessageId = messageId;
        ChannelId = channelId;
        GuildId = guildId;
        Type = type;
        UserId = userId;
        Tag = tag;
    }
}
