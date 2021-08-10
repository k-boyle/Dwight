using System.Threading.Tasks;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dwight.Services
{
    public class PurgingService : DiscordBotService
    {
        protected override async ValueTask OnLeftGuild(LeftGuildEventArgs e)
        {
            Logger.LogInformation("Left guild {Guild}, removing from database", e.Guild.Name);
            
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

            var rep = await context.FwaReps.FindAsync(e.GuildId.RawValue, e.User.Id.RawValue);
            if (rep != null)
                context.Remove(rep);

            context.Remove(member);
            await context.SaveChangesAsync();
        }
    }
}