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
    [Description("Finds all the members that are in the clan but not in the Discord")]
    public async ValueTask<IResult> DiscordCheckAsync()
    {
        var settings = await _dbContext.GetOrCreateSettingsAsync(Context.GuildId, settings => settings.Members);
        var clanMembers = await _clashApiClient.GetClanMembersAsync(settings.ClanTag!, Context.CancellationToken);
        if (clanMembers == null)
            return Response("Clan not found");

        var inClan = settings.Members.SelectMany(member => member.Tags).ToHashSet();

        var missingMembers = clanMembers.Where(member => !inClan.Contains(member.Tag));
        var missingList = string.Join('\n', missingMembers.Select(x => $"{x.Name}{x.Tag}"));

        return Response(string.IsNullOrWhiteSpace(missingList)
            ? "Why are all these people here? There's too many people on this earth. We need a new plague"
            : missingList);
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

    [SlashCommand("alts")]
    [Description("Lists all of the alts in the clan")]
    public async Task<IResult> GetAltsAsync()
    {
        await Deferral();
        
        var settings = await _dbContext.GetOrCreateSettingsAsync(Context.GuildId, settings => settings.Members);
        var members = settings.Members;

        var membersWithAlts = members.Where(member => member.Tags.Length > 1);

        var clanMembers = await _clashApiClient.GetClanMembersAsync(settings.ClanTag!, Context.CancellationToken);
        if (clanMembers == null)
            return Response("Clan not found");

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
            Content = response.Length == 0 ? "No one has alts" : response.ToString()
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