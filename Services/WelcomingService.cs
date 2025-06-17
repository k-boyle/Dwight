using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dwight;

public class WelcomingService : DiscordBotService
{
    private readonly TownhallConfiguration _townhallConfiguration;

    public WelcomingService(IOptions<TownhallConfiguration> townhallConfiguration)
    {
        _townhallConfiguration = townhallConfiguration.Value;
    }

    protected override async ValueTask OnMemberJoined(MemberJoinedEventArgs e)
    {
        await using var scope = Bot.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var context = services.GetDwightDbContext();
        var guildId = e.GuildId;
        var guild = Bot.GetGuild(guildId);

        if (guild is null)
        {
            Logger.LogError("Guild with id {GuildId} not in cache", guildId);
            return;
        }

        var settings = await context.GetOrCreateSettingsAsync(guildId);
            
        if (guild.GetChannel(settings.WelcomeChannelId) is not CachedTextChannel channel)
        {
            Logger.LogWarning("Welcome channel has not been set yet for guild {GuildId}", guildId);
            return;
        }

        var member = e.Member;
            
        if (!guild.Roles.TryGetValue(settings.UnverifiedRoleId, out var role))
            Logger.LogError("Unverified role has not been set yet for guild {GuildId}", guildId);
            
        else
            await member.GrantRoleAsync(role.Id);
            
            
        var embed = CreateWelcomeEmbed(guild, member);
        await channel.SendMessageAsync(new() { Content = member.Mention, Embeds = new List<LocalEmbed> { embed } });
    }

    private LocalEmbed CreateWelcomeEmbed(CachedGuild guild, IMember member)
    {
        var description = new StringBuilder()
            .Append(member.Mention)
            .Append(" welcome to ")
            .Append(guild.Name)
            .Append("!\nBelow are links to FWA approved bases for all Townhalls.\n");

        foreach (var keyValuePair in _townhallConfiguration.BaseLinkByLevel)
        {
            var (level, url) = keyValuePair;
            description.Append(Markdown.Link($"TH{level}", url))
                .Append(", ");
        }

        var bigDescription = $"""
                             And if you're feeling nice post your in game player tag (e.g. #YRQ2Y0UC) so we know who you are!

                             If you're already in the clan you can run a self-verify using the /self-verify command, using your in game API token
                             Settings > More Settings > API Token > Show > Copy
                             And the {Markdown.Link("RCS Password", "https://www.reddit.com/r/RedditClanSystem/wiki/official_reddit_clan_system/")}
                             """;

        description.Append(bigDescription);

        return new()
        {
            Color = new(0x11f711),
            Title = $"Welcome to {guild.Name}!",
            Description = description.ToString()
        };
    }
}