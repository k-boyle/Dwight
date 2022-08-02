using System;
using System.Linq;
using System.Threading.Tasks;
using ClashWrapper;
using ClashWrapper.Entities.ClanMembers;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Dwight;

public class VerificationModule : DiscordApplicationGuildModuleBase
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
    [Description("Verifies the given member with their in game clan tag")]
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

        if (settings.ClanTag == null)
            return Response("Clan tag has not been configured for this clan");

        var clanMembers = await _clashClient.GetClanMembersAsync(settings.ClanTag);

        var clanMember = clanMembers.FirstOrDefault(member => member.Tag.Equals(userTag, StringComparison.CurrentCultureIgnoreCase));

        if (clanMember == null)
            return Response($"{userTag} is not a member of the clan");

        var newMember = new ClashMember(guildId, member.Id, new[] { userTag }, 0, clanMember.Role);

        settings.Members.Add(newMember);

        _dbContext.Update(settings);
        await _dbContext.SaveChangesAsync();

        await member.ModifyAsync(props => props.Nick = clanMember.Name);
        
        var guild = Context.Bot.GetGuild(guildId);
        if (guild == null)
        {
            return Response("Guild not in bot cache, contact your local bot admin");
        }
        
        if (guild.Roles.TryGetValue(settings.UnverifiedRoleId, out var unverifiedRole))
            await member.RevokeRoleAsync(unverifiedRole.Id);

        if (guild.Roles.TryGetValue(settings.VerifiedRoleId, out var verifiedRole))
            await member.GrantRoleAsync(verifiedRole.Id);

        if (guild.GetChannel(settings.GeneralChannelId) is ITextChannel generalChannel)
            await generalChannel.SendMessageAsync(new() { Content = $"{member.Mention} welcome to {guild}. You better learn the rules. If you don't, you'll be eaten in your sleep" });

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

    [AutoComplete("verify")]
    public async ValueTask AutoCompleteVerification(AutoComplete<string> userTag)
    {
        var settings = await _dbContext.GetOrCreateSettingsAsync(Context.GuildId);
        if (settings.ClanTag == null)
        {
            return;
        }

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

    [SlashCommand("unverified")]
    [Description("Lists all of the unverified members in the guild")]
    public async ValueTask<IResult> UnverifiedAsync()
    {
        var guildId = Context.GuildId;
        var guild = Context.Bot.GetGuild(guildId);
        if (guild == null)
        {
            return Response("Guild not in bot cache, contact your local bot admin");
        }
        
        var settings = await _dbContext.GetOrCreateSettingsAsync(guildId);

        if (!guild.Roles.TryGetValue(settings.UnverifiedRoleId, out var role))
            return Response("Unverified role has not been setup yet");

        var unverifiedMembers = guild.Members.Values
            .Where(member => member.RoleIds.Contains(role.Id))
            .Select(member => member.Nick ?? member.Name);

        return Response($"Unverified members:\n{string.Join('\n', unverifiedMembers)}");
    }

    [SlashCommand("add-alt")]
    [RequireAuthorPermissions(Permissions.ManageRoles)]
    [Description("Adds the given player tag to the member")]
    public async ValueTask<IResult> AddTagAsync(IMember member, string tag)
    {
        var guildId = Context.GuildId;
        
        tag = tag.ToUpper();
            
        var settings = await _dbContext.GetOrCreateSettingsAsync(guildId, settings => settings.Members);

        if (settings.ClanTag == null)
        {
            return Response("Clan tag has not been set for this guild");
        }

        var clashMembers = await _clashClient.GetClanMembersAsync(settings.ClanTag);
        var clashMember = clashMembers.FirstOrDefault(member => member.Tag.Equals(tag, StringComparison.CurrentCultureIgnoreCase));

        if (clashMember == null)
            return Response($"A member with tag {tag} does not exist in the clan");

        var alreadyTaken = await _dbContext.Members.FirstOrDefaultAsync(member => member.Tags.Contains(tag));
        if (alreadyTaken != null)
            return Response($"Identity theft is not a joke, {Context.Author.Mention}");

        var clanMember = await _dbContext.Members.FindAsync(guildId, member.Id.RawValue);
        if (clanMember == null)
            return Response($"{member} has not been verified");

        if (clanMember.Tags.Contains(tag, StringComparer.CurrentCultureIgnoreCase))
            return Response($"{member} already has tag {tag}");

        clanMember.Tags = clanMember.Tags.Append(tag).ToArray();
        _dbContext.Update(clanMember);
        await _dbContext.SaveChangesAsync();

        return Response($"Added tag {tag} to {member}");
    }
        
    [SlashCommand("remove-alt")]
    [RequireAuthorPermissions(Permissions.ManageRoles)]
    [Description("Removes the given player tag from the member")]
    public async ValueTask<IResult> RemoveTagAsync(IMember member, string tag)
    {
        tag = tag.ToUpper();
            
        var clanMember = await _dbContext.Members.FindAsync(Context.GuildId, member.Id.RawValue);
        if (clanMember == null)
            return Response($"{member} has not been verified");

        if (clanMember.Tags.Length == 1)
            return Response($"{tag} is the last tag that {member} has, cannot remove it");
            
        if (!clanMember.Tags.Contains(tag, StringComparer.CurrentCultureIgnoreCase))
            return Response($"{member} doesn't have tag {tag}");

        clanMember.Tags = clanMember.Tags.Where(other => !other.Equals(tag, StringComparison.CurrentCultureIgnoreCase)).ToArray();
        _dbContext.Update(clanMember);
        await _dbContext.SaveChangesAsync();

        return Response($"Removed tag {tag} to {member}");
    }
}