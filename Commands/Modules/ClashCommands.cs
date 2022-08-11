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
    [RequireClanTag]
    [Description("Gets the current members in the clan ordered by donations")]
    public async ValueTask<IResult> ViewMembersAsync()
    {
        var settings = await _dbContext.GetOrCreateSettingsAsync(Context.GuildId);
        var clanTag = settings.ClanTag!;

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

    [SlashCommand("discord-check")]
    [RequireClanTag]
    [Description("Finds all the members that are in the clan but not in the Discord")]
    public async ValueTask<IResult> DiscordCheckAsync()
    {
        var settings = await _dbContext.GetOrCreateSettingsAsync(Context.GuildId, settings => settings.Members);
        var clanMembers = await _clashClient.GetClanMembersAsync(settings.ClanTag!);
        var inClan = settings.Members.SelectMany(member => member.Tags).ToHashSet();

        var missingMembers = clanMembers.Where(member => !inClan.Contains(member.Tag));
        var missingList = string.Join('\n', missingMembers.Select(x => $"{x.Name}{x.Tag}"));

        return Response(string.IsNullOrWhiteSpace(missingList) ? "Why are all these people here? There's too many people on this earth. We need a new plague" : missingList);
    }

    [SlashCommand("reminders")]
    [Description("Set whether you want attack reminders in farm wars")]
    public async Task<IResult> RemindersAsync(bool remind)
    {
        var member = await _dbContext.Members.FindAsync(Context.GuildId.RawValue, Context.Author.Id.RawValue);
        if (member == null)
            return Response("You are not part of a clan");

        if (member.Remind != remind)
        {
            member.Remind = remind;
            _dbContext.Members.Update(member);
            await _dbContext.SaveChangesAsync();
        }

        return Response(remind ? "You will now receive reminders to attack in farm wars" : "You will no longer receive reminders to attack in farm wars");
    }
}