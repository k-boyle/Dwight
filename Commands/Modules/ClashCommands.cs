using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Dwight;

public class ClashCommands : DiscordApplicationGuildModuleBase
{
    private readonly ClashApiClient _clashApiClient;
    private readonly DwightDbContext _dbContext;

    public ClashCommands(ClashApiClient clashApiClient, DwightDbContext dbContext)
    {
        _clashApiClient = clashApiClient;
        _dbContext = dbContext;
    }

    [SlashCommand("discord-check")]
    [RequireClanTag]
    [Description("Roots out everyone in the clan who has not reported to the Discord")]
    public async ValueTask<IResult> DiscordCheckAsync()
    {
        var settings = await _dbContext.GetOrCreateSettingsAsync(Context.GuildId, settings => settings.Members);
        var clanMembers = await _clashApiClient.GetClanMembersAsync(settings.ClanTag!, Context.CancellationToken);
        if (clanMembers == null)
            return Response("Clan not found. I searched. I am thorough. It is not there.");

        var inClan = settings.Members.SelectMany(member => member.Tags).ToHashSet();

        var missingMembers = clanMembers.Where(member => !inClan.Contains(member.Tag));
        var missingList = string.Join('\n', missingMembers.Select(x => $"{x.Name}{x.Tag}"));

        return Response(string.IsNullOrWhiteSpace(missingList)
            ? "Why are all these people here? There's too many people on this earth. We need a new plague"
            : missingList);
    }

    [SlashCommand("reminders")]
    [Description("Decide whether I will hound you to attack in farm wars. I recommend yes.")]
    public async Task<IResult> RemindersAsync(bool remind)
    {
        var member = await _dbContext.Members.FindAsync(Context.GuildId.RawValue, Context.Author.Id.RawValue);
        if (member == null)
            return Response("You are not part of a clan. I do not take orders from civilians.");

        if (member.Remind != remind)
        {
            member.Remind = remind;
            _dbContext.Members.Update(member);
            await _dbContext.SaveChangesAsync();
        }

        return Response(remind ? "Good. I will remind you to attack in farm wars. Discipline is everything." : "Fine. I will no longer remind you. Your missed attacks are now your own shame.");
    }

    [SlashCommand("alts")]
    [Description("Exposes every member running more than one account in the clan")]
    public async Task<IResult> GetAltsAsync()
    {
        await Deferral();
        
        var settings = await _dbContext.GetOrCreateSettingsAsync(Context.GuildId, settings => settings.Members);
        var members = settings.Members;

        var membersWithAlts = members.Where(member => member.Tags.Length > 1);

        var clanMembers = await _clashApiClient.GetClanMembersAsync(settings.ClanTag!, Context.CancellationToken);
        if (clanMembers == null)
            return Response("Clan not found. I searched. I am thorough. It is not there.");

        var response = new StringBuilder();

        foreach (var member in membersWithAlts)
        {
            var altsInClan = clanMembers.Where(clanMember => member.Tags.Any(tag => tag == clanMember.Tag)).ToList();
            if (altsInClan.Count <= 1) continue;

            response.AppendLine(Mention.User(member.DiscordId));
            foreach (var (tag, name, _, _) in altsInClan)
            {
                response.AppendLine($"- {tag}: {name}");
            }
        }

        var messageResponse = new LocalInteractionMessageResponse
        {
            AllowedMentions = LocalAllowedMentions.None,
            Content = response.Length == 0 ? "No one is running alts. An honest clan. How refreshing." : response.ToString()
        };
        return Response(messageResponse);
    }

    [SlashCommand("password")]
    [Description("Hack the mainframe")]
    public IResult HackMainframeAsync()
    {
        return Response("https://www.reddit.com/r/RedditClanSystem/wiki/official_reddit_clan_system/");
    }
}