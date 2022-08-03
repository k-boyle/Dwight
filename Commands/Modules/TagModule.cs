using System;
using System.Linq;
using System.Threading.Tasks;
using ClashWrapper;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Dwight;

[SlashGroup("tag")]
public partial class TagModule : DiscordApplicationGuildModuleBase
{
    private readonly ClashClient _clashClient;
    private readonly DwightDbContext _dbContext;

    public TagModule(ClashClient clashClient, DwightDbContext dbContext)
    {
        _clashClient = clashClient;
        _dbContext = dbContext;
    }
    
    [SlashCommand("add")]
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

    [SlashCommand("remove")]
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

    [SlashCommand("set")]
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

        return Response($"Set main of {member.Mention}");
    }
}