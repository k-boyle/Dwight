using System;
using System.Threading.Tasks;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Dwight;

[SlashGroup("activity")]
[RequireBotOwner]
public class ActivityModule : DiscordApplicationGuildModuleBase
{
    private readonly DwightDbContext _dbContext;
    private readonly ActivityCollectionService _collectionService;

    public ActivityModule(DwightDbContext dbContext, ActivityCollectionService collectionService)
    {
        _dbContext = dbContext;
        _collectionService = collectionService;
    }

    [SlashCommand("collect")]
    [RequireClanTag]
    [Description("Conducts surveillance on the clan by hand. Nothing escapes my notice.")]
    public async Task<IResult> CollectAsync()
    {
        await Deferral();

        var settings = await _dbContext.GuildSettings.FindAsync(Context.GuildId.RawValue);
        var count = await _collectionService.CollectAsync(settings!, DateTimeOffset.UtcNow, Context.CancellationToken);

        return Response($"Surveillance complete. {count} sample(s) filed. The intelligence is mine.");
    }
}
