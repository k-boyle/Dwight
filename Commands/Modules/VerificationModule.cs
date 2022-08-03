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
            return Response("Guild not in bot cache, contact your local bot admin");

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

    [SlashCommand("add-alt")]
    [RequireAuthorPermissions(Permissions.ManageRoles)]
    [Description("Adds the given player tag to the member")]
    public async ValueTask<IResult> AddAltAsync(IMember member, string tag)
    {
        var guildId = Context.GuildId;
        
        tag = tag.ToUpper();

        var settings = await _dbContext.GetOrCreateSettingsAsync(guildId, settings => settings.Members);

        if (settings.ClanTag == null)
            return Response("Clan tag has not been set for this guild");

        var clashMembers = await _clashClient.GetClanMembersAsync(settings.ClanTag);
        var clashMember = clashMembers.FirstOrDefault(member => member.Tag.Equals(tag, StringComparison.CurrentCultureIgnoreCase));

        if (clashMember == null)
            return Response($"A member with tag {tag} does not exist in the clan");

        var alreadyTaken = await _dbContext.Members.FirstOrDefaultAsync(member => member.Tags.Contains(tag));
        if (alreadyTaken != null)
            return Response($"Identity theft is not a joke, {Context.Author.Mention}");

        var clanMember = await _dbContext.Members.FindAsync(guildId.RawValue, member.Id.RawValue);
        if (clanMember == null)
            return Response($"{member.Mention} has not been verified");

        if (clanMember.Tags.Contains(tag, StringComparer.CurrentCultureIgnoreCase))
            return Response($"{member.Mention} already has tag {tag}");

        clanMember.Tags = clanMember.Tags.Append(tag).ToArray();
        _dbContext.Update(clanMember);
        await _dbContext.SaveChangesAsync();

        return Response($"Added tag {tag} to {member.Mention}");
    }

    [SlashCommand("remove-alt")]
    [RequireAuthorPermissions(Permissions.ManageRoles)]
    [Description("Removes the given player tag from the member")]
    public async ValueTask<IResult> RemoveAltAsync(IMember member, string tag)
    {
        tag = tag.ToUpper();

        var clashMember = await _dbContext.Members.FindAsync(Context.GuildId.RawValue, member.Id.RawValue);
        if (clashMember == null)
            return Response($"{member.Mention} has not been verified");

        if (clashMember.Tags.Length == 1)
            return Response($"{tag} is the last tag that {member.Mention} has, cannot remove it");
            
        if (!clashMember.Tags.Contains(tag, StringComparer.CurrentCultureIgnoreCase))
            return Response($"{member.Mention} doesn't have tag {tag}");

        if (Array.FindIndex(clashMember.Tags, str => str.Equals(tag, StringComparison.InvariantCultureIgnoreCase)) == clashMember.MainTag)
            return Response($"{tag} is their main account, use the \"main\" command to update it first");

        clashMember.Tags = clashMember.Tags.Where(other => !other.Equals(tag, StringComparison.CurrentCultureIgnoreCase)).ToArray();
        _dbContext.Update(clashMember);
        await _dbContext.SaveChangesAsync();

        return Response($"Removed tag {tag} to {member.Mention}");
    }

    [SlashCommand("main")]
    [RequireAuthorPermissions(Permissions.ManageRoles)]
    [RequireBotPermissions(Permissions.ManageNicks)]
    [Description("Sets the main account of the member")]
    public async ValueTask<IResult> SetMainAsync(IMember member, string tag)
    {
        tag = tag.ToUpper();
        
        var settings = await _dbContext.GetOrCreateSettingsAsync(Context.GuildId, settings => settings.Members);
        if (settings.ClanTag == null)
            return Response("Clan tag has not been set for this guild");
        
        var clanMembers = await _clashClient.GetClanMembersAsync(settings.ClanTag);
        var clanMember = clanMembers.FirstOrDefault(member => member.Tag.Equals(tag));

        if (clanMember == null)
            return Response($"{tag} is not in the clan");
        
        var clashMember = await _dbContext.Members.FindAsync(Context.GuildId.RawValue, member.Id.RawValue);
        if (clashMember == null)
            return Response($"{member.Mention} has not been verified");

        if (clashMember.Tags.Length == 1)
            return Response($"{tag} is the only tag that {member.Mention} has");

        if (!clashMember.Tags.Contains(tag, StringComparer.CurrentCultureIgnoreCase))
            return Response($"{member.Mention} doesn't have tag {tag}");

        var tagIndex = Array.FindIndex(clashMember.Tags, str => str.Equals(tag, StringComparison.InvariantCultureIgnoreCase));
        if (tagIndex == clashMember.MainTag)
            return Response($"{tag} is already their main account");

        await member.ModifyAsync(properties => properties.Nick = clanMember.Name);

        clashMember.MainTag = tagIndex;
        _dbContext.Update(clashMember);
        await _dbContext.SaveChangesAsync();

        return Response($"Set main of {member}");
    }
}