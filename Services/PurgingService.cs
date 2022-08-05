using System.Threading;
using System.Threading.Tasks;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dwight;

public class PurgingService : DiscordBotService
{
    // todo periodic purging?
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Bot.WaitUntilReadyAsync(stoppingToken);

        await using var scope = Bot.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetDwightDbContext();

        Logger.LogInformation("Purging guilds");
        var guilds = Bot.GetGuilds();
        // var notIn = await context.GuildSettings.Where(settings => !guilds.ContainsKey(settings.GuildId))
        //     .ToListAsync(stoppingToken);
        //
        // Logger.LogInformation("Found {Count} guilds to purge", notIn.Count);
        //
        // if (notIn.Count == 0)
        //     return;
        //
        // context.GuildSettings.RemoveRange(notIn);
        // await context.SaveChangesAsync(stoppingToken);
    }

    protected override async ValueTask OnLeftGuild(LeftGuildEventArgs e)
    {
        Logger.LogInformation("Left guild {Guild}, removing from database", e.GuildId);
            
        await using var scope = Bot.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetDwightDbContext();

        var settings = await context.GuildSettings.FindAsync(e.GuildId.RawValue);
        if (settings == null)
            return;
            
        context.Remove(settings);
        await context.SaveChangesAsync();
    }

    protected override async ValueTask OnMemberLeft(MemberLeftEventArgs e)
    {
        Logger.LogInformation("User {User} left guild {Guild}, removing from database", e.User, e.GuildId);
            
        await using var scope = Bot.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetDwightDbContext();

        var member = await context.Members.FindAsync(e.GuildId.RawValue, e.User.Id.RawValue);
        if (member == null)
            return;

        context.Remove(member);
        await context.SaveChangesAsync();
    }
}