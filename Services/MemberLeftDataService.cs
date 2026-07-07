using System.Linq;
using System.Threading.Tasks;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dwight;

/// <summary>
/// When a member abandons a guild, Dwight purges their file: the account link
/// (<see cref="ClashMember"/>) and any persisted views tied to their user id. Activity samples and
/// seen clan members are keyed on in-game player tags rather than the Discord identity, so they are
/// clan records and left untouched.
/// </summary>
public class MemberLeftDataService : DiscordBotService
{
    protected override async ValueTask OnMemberLeft(MemberLeftEventArgs e)
    {
        ulong guildId = e.GuildId;
        ulong discordId = e.MemberId;

        await using var scope = Bot.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetDwightDbContext();

        var removedMembers = await context.Members
            .Where(member => member.GuildId == guildId && member.DiscordId == discordId)
            .ExecuteDeleteAsync();

        var removedViews = await context.PersistedViews
            .Where(view => view.GuildId == guildId && view.UserId == discordId)
            .ExecuteDeleteAsync();

        if (removedMembers > 0 || removedViews > 0)
            Logger.LogInformation(
                "Member {DiscordId} left guild {GuildId}; purged {MemberCount} member record(s) and {ViewCount} persisted view(s)",
                discordId, guildId, removedMembers, removedViews);
    }
}
