using System.Linq;
using System.Threading.Tasks;
using Disqord.Bot.Commands.Application;

namespace Dwight;

public partial class VerificationModule
{
    [AutoComplete("verify")]
    public async ValueTask VerifyAsync(AutoComplete<string> userTag)
    {
        var settings = await _dbContext.GetOrCreateSettingsAsync(Context.GuildId, settings => settings.Members);
        if (settings.ClanTag == null)
            return;

        // if (member.IsFocused)
        // {
        //     var guild = Context.Bot.GetGuild(Context.GuildId);
        //     if (guild == null)
        //     {
        //         return;
        //     }
        //
        //     var inGuildIds = settings.Members.Select(member => member.DiscordId).ToHashSet();
        //     var unverified = guild.Members.Values.Where(member => !inGuildIds.Contains(member.Id));
        //     member.Choices.AddRange(unverified);
        // }

        if (userTag.IsFocused)
        {
            var inClan = settings.Members.SelectMany(member => member.Tags).ToHashSet();
            var members = await _clashClient.GetClanMembersAsync(settings.ClanTag);
            var notInClan = members.Where(member => !inClan.Contains(member.Tag)).Select(member => member.Tag).Take(25);
            userTag.Choices.AddRange(notInClan);
        }
    }
}