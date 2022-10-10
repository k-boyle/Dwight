using System.Linq;
using System.Threading.Tasks;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Dwight;

[RequireBotOwner]
public class CasinoModule : DiscordApplicationGuildModuleBase
{
    private readonly DwightDbContext _dbContext;

    public CasinoModule(DwightDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [SlashCommand("march21st")]
    [Description("A 2013 American anthology")]
    public async Task<IResult> PurgeAsync()
    {
        var members = await Context.Bot.FetchMembersAsync(Context.GuildId, cancellationToken: Context.CancellationToken);
        var memberIds = members.Select(member => member.Id.RawValue).ToHashSet();
        int purged = 0;

        var guildId = Context.GuildId.RawValue;
        var savedMembers = _dbContext.Members.Where(member => member.GuildId == guildId);
        await foreach (var saved in savedMembers.AsAsyncEnumerable().WithCancellation(Context.CancellationToken))
        {
            if (memberIds.Contains(saved.DiscordId))
                continue;
            
            purged++;
            _dbContext.Members.Remove(saved);
        }

        await _dbContext.SaveChangesAsync(Context.CancellationToken);

        return Response($"It is now March 22nd, {purged} has been purged");
    }
}