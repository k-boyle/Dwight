using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClashWrapper;
using ClashWrapper.Entities.ClanMembers;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Gateway;
using Disqord.Rest;
using Qmmands;

namespace Dwight;

public partial class VerificationModule : DiscordApplicationGuildModuleBase
{
    private readonly ClashClient _clashClient;
    private readonly DwightDbContext _dbContext;

    public VerificationModule(ClashClient clashClient, DwightDbContext dbContext)
    {
        _clashClient = clashClient;
        _dbContext = dbContext;
    }

    [SlashCommand("verify")]
    [RequireBotPermissions(Permissions.ManageRoles)]
    [RequireBotPermissions(Permissions.SetNick)]
    [RequireAuthorPermissions(Permissions.ManageRoles)]
    [RequireClanTag]
    [Description("Verifies the given member with their in game player tag")]
    public async ValueTask<IResult> VerifyAsync(IMember member, string userTag)
    {
        userTag = userTag.ToUpper();

        var guildId = Context.GuildId;
        var settings = await _dbContext.GetOrCreateSettingsAsync(guildId, settings => settings.Members);

        foreach (var guildMember in settings.Members)
        {
            if (guildMember.DiscordId == member.Id)
                return Response($"{member} is already verified");

            if (guildMember.Tags.Contains(userTag, StringComparer.CurrentCultureIgnoreCase))
                return Response($"Identity theft is not a joke, {Context.Author.Mention}");
        }

        var clanMembers = await _clashClient.GetClanMembersAsync(settings.ClanTag!);

        var clanMember = clanMembers.FirstOrDefault(member => member.Tag.Equals(userTag, StringComparison.CurrentCultureIgnoreCase));

        if (clanMember == null)
            return Response($"{userTag} is not a member of the clan");

        var newMember = new ClashMember(guildId, member.Id, new[] { userTag }, 0, clanMember.Role, false);

        settings.Members.Add(newMember);

        _dbContext.Update(settings);
        await _dbContext.SaveChangesAsync();

        await member.ModifyAsync(props => props.Nick = clanMember.Name);

        var guild = Context.Bot.GetGuild(guildId);
        if (guild == null)
            return Response("Guild not in bot cache, contact your local bot admin");

        if (guild.Roles.TryGetValue(settings.UnverifiedRoleId, out var unverifiedRole))
            await member.RevokeRoleAsync(unverifiedRole.Id);

        if (guild.Roles.TryGetValue(settings.VerifiedRoleId, out var verifiedRole))
            await member.GrantRoleAsync(verifiedRole.Id);

        if (guild.GetChannel(settings.GeneralChannelId) is ITextChannel generalChannel)
            await generalChannel.SendMessageAsync(new()
                { Content = $"{member.Mention} welcome to {guild.Name}. You better learn the rules. If you don't, you'll be eaten in your sleep" });

        var roleId = clanMember.Role switch
        {
            ClanRole.CoLeader => settings.CoLeaderRoleId,
            ClanRole.Elder => settings.ElderRoleId,
            _ => 0UL
        };

        if (guild.Roles.TryGetValue(roleId, out _))
            await member.GrantRoleAsync(roleId);

        return Response("Member has been verified");
    }

    [MessageCommand("Verify")]
    [RequireBotPermissions(Permissions.ManageRoles)]
    [RequireBotPermissions(Permissions.SetNick)]
    [RequireAuthorPermissions(Permissions.ManageRoles)]
    [RequireClanTag]
    [Description("Verifies the user who posted the message")]
    public async ValueTask<IResult> VerifyAsync(IMessage message)
    {
        var tagRegex = new Regex("(?<tag>#[A-Z0-9]{8})", RegexOptions.Compiled);
        var match = tagRegex.Match(message.Content);
        if (!match.Success)
            return Response(new LocalInteractionMessageResponse { Content = "Failed to find a player tag in the message", IsEphemeral = true });

        var group = match.Groups["tag"];
        var tag = group.Value;
        var member = await Context.Bot.FetchMemberAsync(Context.GuildId, message.Author.Id);
        
        return await VerifyAsync(member!, tag);
    }

    [SlashCommand("unverified")]
    [Description("Lists all of the unverified members in the guild")]
    public async ValueTask<IResult> UnverifiedAsync()
    {
        var guildId = Context.GuildId;
        var guild = Context.Bot.GetGuild(guildId);
        if (guild == null)
            return Response("Guild not in bot cache, contact your local bot admin");

        var settings = await _dbContext.GetOrCreateSettingsAsync(guildId);

        if (!guild.Roles.TryGetValue(settings.UnverifiedRoleId, out var role))
            return Response("Unverified role has not been setup yet");

        var unverifiedMembers = guild.Members.Values
            .Where(member => member.RoleIds.Contains(role.Id))
            .Select(member => member.Nick ?? member.Name);

        return Response($"Unverified members:\n{string.Join('\n', unverifiedMembers)}");
    }
}