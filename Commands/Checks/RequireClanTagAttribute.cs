using System.Threading.Tasks;
using Disqord.Bot.Commands;
using Qmmands;

namespace Dwight;

public class RequireClanTagAttribute : DiscordCheckAttribute
{
    public override async ValueTask<IResult> CheckAsync(IDiscordCommandContext context)
    {
        if (!context.GuildId.HasValue)
            return Results.Failure("Command can only be executed within a guild context");
        
        var dbContext = context.Services.GetDwightDbContext();
        var settings = await dbContext.GuildSettings.FindAsync(context.GuildId.Value.RawValue);
        
        if (settings?.ClanTag == null)
            return Results.Failure("Clan tag needs to be set to execute this command");

        return Results.Success;
    }
}
