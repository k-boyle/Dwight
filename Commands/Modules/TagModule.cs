using System;
using System.Linq;
using System.Threading.Tasks;
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
    private readonly ClashApiClient _clashApiClient;
    private readonly DwightDbContext _dbContext;

    public TagModule(ClashApiClient clashApiClient, DwightDbContext dbContext)
    {
        _clashApiClient = clashApiClient;
        _dbContext = dbContext;
    }
    
    [SlashCommand("add")]
    [RequireAuthorPermissions(Permissions.ManageRoles)]
    [RequireClanTag]
    [Description("Registers another account under a member. Alts must be declared.")]
    public async ValueTask<IResult> AddAltAsync(IMember member, string tag)
    {
        var guildId = Context.GuildId;
        var settings = await _dbContext.GetOrCreateSettingsAsync(guildId, settings => settings.Members);

        var clashMembers = await _clashApiClient.GetClanMembersAsync(settings.ClanTag!, Context.CancellationToken);
        if (clashMembers == null)
            return Response("Clan not found. I searched. I am thorough. It is not there.");

        var clashMember = clashMembers.FirstOrDefault(member => member.Tag.Equals(tag, StringComparison.CurrentCultureIgnoreCase));

        if (clashMember == null)
            return Response($"No member with tag {tag} exists in this clan. I do not file paperwork for ghosts.");

        var alreadyTaken = await _dbContext.Members.FirstOrDefaultAsync(member => member.Tags.Contains(tag));
        if (alreadyTaken != null)
            return Response($"Identity theft is not a joke, {Context.Author.Mention}");

        var clanMember = await _dbContext.Members.FindAsync(guildId.RawValue, member.Id.RawValue);
        if (clanMember == null)
            return Response($"{member.Mention} has not been verified. Prove who they are first.");

        if (clanMember.Tags.Contains(tag, StringComparer.CurrentCultureIgnoreCase))
            return Response($"{member.Mention} already holds tag {tag}. My records are impeccable.");

        clanMember.Tags = clanMember.Tags.Append(tag).ToArray();
        _dbContext.Update(clanMember);
        await _dbContext.SaveChangesAsync();

        return Response($"Tag {tag} is now registered to {member.Mention}. It has been recorded.");
    }

    [SlashCommand("remove")]
    [RequireAuthorPermissions(Permissions.ManageRoles)]
    [Description("Strikes a player tag from a member's record")]
    public async ValueTask<IResult> RemoveAltAsync(IMember member, string tag)
    {
        var clashMember = await _dbContext.Members.FindAsync(Context.GuildId.RawValue, member.Id.RawValue);
        if (clashMember == null)
            return Response($"{member.Mention} has not been verified. There is nothing to remove.");

        if (clashMember.Tags.Length == 1)
            return Response($"{tag} is the last tag {member.Mention} has. I will not leave a man with nothing.");

        if (!clashMember.Tags.Contains(tag, StringComparer.CurrentCultureIgnoreCase))
            return Response($"{member.Mention} does not have tag {tag}. You cannot remove what does not exist.");

        if (Array.FindIndex(clashMember.Tags, str => str.Equals(tag, StringComparison.InvariantCultureIgnoreCase)) == clashMember.MainTag)
            return Response($"{tag} is their main account. Use the \"main\" command to reassign it first. Order matters.");

        clashMember.Tags = clashMember.Tags.Where(other => !other.Equals(tag, StringComparison.CurrentCultureIgnoreCase)).ToArray();
        _dbContext.Update(clashMember);
        await _dbContext.SaveChangesAsync();

        return Response($"Tag {tag} has been struck from {member.Mention}'s record.");
    }

    [SlashCommand("set")]
    [RequireAuthorPermissions(Permissions.ManageRoles)]
    [RequireBotPermissions(Permissions.ManageNicks)]
    [RequireClanTag]
    [Description("Declares which account is a member's primary. There can be only one.")]
    public async ValueTask<IResult> SetMainAsync(IMember member, string tag)
    {
        var settings = await _dbContext.GetOrCreateSettingsAsync(Context.GuildId, settings => settings.Members);
        if (settings.ClanTag == null)
            return Response("No clan tag has been set for this guild. I cannot work without a clan.");

        var clanMembers = await _clashApiClient.GetClanMembersAsync(settings.ClanTag, Context.CancellationToken);
        if (clanMembers == null)
            return Response("Clan not found. I searched. I am thorough. It is not there.");

        var clanMember = clanMembers.FirstOrDefault(member => member.Tag.Equals(tag, StringComparison.CurrentCultureIgnoreCase));

        if (clanMember == null)
            return Response($"{tag} is not in the clan. I do not promote outsiders.");

        var clashMember = await _dbContext.Members.FindAsync(Context.GuildId.RawValue, member.Id.RawValue);
        if (clashMember == null)
            return Response($"{member.Mention} has not been verified. Prove who they are first.");

        if (clashMember.Tags.Length == 1)
            return Response($"{tag} is the only tag {member.Mention} has. It is already their main by default.");

        if (!clashMember.Tags.Contains(tag, StringComparer.CurrentCultureIgnoreCase))
            return Response($"{member.Mention} does not have tag {tag}. You cannot promote what they do not own.");

        var tagIndex = Array.FindIndex(clashMember.Tags, str => str.Equals(tag, StringComparison.InvariantCultureIgnoreCase));
        if (tagIndex == clashMember.MainTag)
            return Response($"{tag} is already their main account. Stop wasting my time.");

        await member.ModifyAsync(properties => properties.Nick = clanMember.Name);

        clashMember.MainTag = tagIndex;
        _dbContext.Update(clashMember);
        await _dbContext.SaveChangesAsync();

        return Response($"{member.Mention}'s main account is now {tag}. The hierarchy is restored.");
    }

    [SlashCommand("whomst")]
    [Description("Determines, beyond doubt, who in the clan owns a given account")]
    [RequireClanTag]
    public async ValueTask<IResult> WhomstAsync(string name)
    {
        var settings = await _dbContext.GetOrCreateSettingsAsync(Context.GuildId, settings => settings.Members);

        var clashMembers = await _clashApiClient.GetClanMembersAsync(settings.ClanTag!, Context.CancellationToken);
        var foundMembers = clashMembers!.Where(member => member.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
            .Select(member => member.Tag)
            .ToHashSet();

        if (foundMembers.Count == 0)
            return Response($"No one in the clan goes by {name}. I would know.");

        var clanMembers = settings.Members.Where(member => member.Tags.Any(foundMembers.Contains))
            .Select(member => member.DiscordId)
            .ToList();

        if (clanMembers.Count == 0)
            return Response($"No one in this Discord owns {name}. The account is unclaimed. Suspicious.");

        var response = clanMembers.Select(id => $"{name} belongs to {Mention.User(id)}. Fact.");
        return Response(string.Join('\n', response));
    }

    [SlashCommand("list")]
    [Description("Produces the full record of every account a member has declared")]
    [RequireClanTag]
    public async ValueTask<IResult> ListTagsAsync(IMember member)
    {
        var clashMember = await _dbContext.Members.FindAsync(Context.GuildId.RawValue, member.Id.RawValue);
        if (clashMember == null)
            return Response($"{member.Mention} has no verified account in this clan. They are a phantom.");

        return Response(string.Join(' ', clashMember.Tags));
    }
}