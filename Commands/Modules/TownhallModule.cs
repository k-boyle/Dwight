using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Dwight;

[SlashGroup("townhall")]
public class TownhallModule : DiscordApplicationGuildModuleBase
{
    private readonly DwightDbContext _dbContext;

    public TownhallModule(DwightDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [SlashCommand("set")]
    [RequireAuthorPermissions(Permissions.ManageRoles)]
    [Description("Sanctions, or replaces, the approved base layout for a Townhall level. No re-deploy required.")]
    public async ValueTask<IResult> SetAsync(int townhall, string link)
    {
        var existing = await _dbContext.TownhallBases.FindAsync(Context.GuildId.RawValue, townhall);
        if (existing == null)
            await _dbContext.TownhallBases.AddAsync(new TownhallBase(Context.GuildId, townhall, link));
        else
            existing.Link = link;

        await _dbContext.SaveChangesAsync();

        return Response($"Townhall {townhall} is now sanctioned to {link}. Let it be known.");
    }

    [SlashCommand("remove")]
    [RequireAuthorPermissions(Permissions.ManageRoles)]
    [Description("Revokes the approved base for a Townhall level")]
    public async ValueTask<IResult> RemoveAsync(int townhall)
    {
        var existing = await _dbContext.TownhallBases.FindAsync(Context.GuildId.RawValue, townhall);
        if (existing == null)
            return Response($"Townhall {townhall} has no sanctioned base to revoke. Nothing to do.");

        _dbContext.TownhallBases.Remove(existing);
        await _dbContext.SaveChangesAsync();

        return Response($"Townhall {townhall}'s sanction is revoked. Anarchy resumes at that level.");
    }

    [SlashCommand("list")]
    [Description("Lists every sanctioned base, by Townhall level")]
    public async ValueTask<IResult> ListAsync()
    {
        var bases = await _dbContext.TownhallBases
            .Where(townhallBase => townhallBase.GuildId == Context.GuildId.RawValue)
            .OrderByDescending(townhallBase => townhallBase.Level)
            .ToListAsync();

        if (bases.Count == 0)
            return Response("There are no sanctioned bases on record. A regrettable state of affairs.");

        var response = new StringBuilder();
        foreach (var townhallBase in bases)
            response.AppendLine($"TH{townhallBase.Level}: <{townhallBase.Link}>");

        return Response(response.ToString());
    }
}
