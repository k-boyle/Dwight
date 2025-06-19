using System.Threading.Tasks;
using Disqord.Bot.Hosting;
using Disqord.Extensions.Interactivity.Menus;
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

        if (settings.Password == null)
        {
            Logger.LogWarning("Password has not been set yet for guild {GuildId}", guildId);
            return;
        }

        var member = e.Member;
            
        if (!guild.Roles.TryGetValue(settings.UnverifiedRoleId, out var role))
            Logger.LogError("Unverified role has not been set yet for guild {GuildId}", guildId);
            
        else
            await member.GrantRoleAsync(role.Id);

        var welcomeView = new WelcomeView(
            guild.Name,
            _townhallConfiguration.BaseLinkByLevel,
            member.Id,
            settings.Password
        );
        var menu = new DefaultTextMenu(welcomeView);
        await Bot.StartMenuAsync(channel.Id, menu);
    }
}