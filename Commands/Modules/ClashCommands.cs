using System.Linq;
using System.Threading.Tasks;
using ClashWrapper;
using ClashWrapper.Entities.War;
using Disqord;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Dwight;

public class ClashCommands : DiscordApplicationGuildModuleBase
{
    private readonly ClashClient _clashClient;
    private readonly DwightDbContext _dbContext;

    public ClashCommands(ClashClient clashClient, DwightDbContext dbContext)
    {
        _clashClient = clashClient;
        _dbContext = dbContext;
    }

    [SlashCommand("members")]
    [Description("Gets the current members in the clan ordered by donations")]
    public async ValueTask<IResult> ViewMembersAsync()
    {
        var settings = await _dbContext.GetOrCreateSettingsAsync(Context.GuildId);
        var clanTag = settings.ClanTag;

        if (clanTag == null)
        {
            return Response("You need to set a clan tag for this guild");
        }

        var members = await _clashClient.GetClanMembersAsync(clanTag);
        var orderedByDonation = members.OrderByDescending(member => member.Donations);

        var currentWar = await _clashClient.GetCurrentWarAsync(clanTag);
        var missedAttackers = currentWar.Clan.Members.Where(member => member.Attacks.Count == 0);

        var membersDonationsString = string.Join("\n", orderedByDonation.Select((member, index) => $"{index + 1}: {Markdown.Escape(member.Name)} - {member.Donations}"));
        var responseString = $"{Markdown.Bold(Markdown.Underline("Members:"))}\n{membersDonationsString}";

        if (currentWar.State == WarState.Ended)
        {
            var missedAttacksString = string.Join("\n", missedAttackers.Select(member => Markdown.Escape(member.Name)));
            responseString = $"{responseString}\n\n{Markdown.Bold(Markdown.Underline("Missed Attackers:"))}\n{missedAttacksString}";
        }

        return Response(responseString);
    }

    // todo precondition for clantag
    [SlashCommand("discord-check")]
    [Description("Finds all the members that are in the clan but not in the Discord")]
    public async ValueTask<IResult> DiscordCheckAsync()
    {
        var settings = await _dbContext.GetOrCreateSettingsAsync(Context.GuildId, settings => settings.Members);
        var clanTag = settings.ClanTag;

        if (clanTag == null)
        {
            return Response("You need to set a clan tag for this guild");
        }
        
        var clanMembers = await _clashClient.GetClanMembersAsync(settings.ClanTag!);
        var inClan = settings.Members.SelectMany(member => member.Tags).ToHashSet();

        var missingMembers = clanMembers.Where(member => !inClan.Contains(member.Tag));
        var missingList = string.Join('\n', missingMembers.Select(x => $"{x.Name}{x.Tag}"));

        return Response(missingList);
    }
}