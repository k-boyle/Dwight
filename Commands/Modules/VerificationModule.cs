using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.Extensions.Options;
using Qmmands;

namespace Dwight;

public partial class VerificationModule(
    ClashApiClient clashApiClient,
    DwightDbContext dbContext,
    IOptions<TownhallConfiguration> townhallConfiguration)
    : DiscordApplicationGuildModuleBase
{
    [SlashCommand("verify")]
    [RequireBotPermissions(Permissions.ManageRoles)]
    [RequireBotPermissions(Permissions.SetNick)]
    [RequireAuthorPermissions(Permissions.ManageRoles)]
    [RequireClanTag]
    [Description("Confirms a member's identity with their in game player tag. Identity theft is not a joke.")]
    public async ValueTask<IResult> VerifyAsync(IMember member, string userTag)
    {
        await Deferral();

        userTag = userTag.ToUpper();

        var guildId = Context.GuildId;
        var settings = await dbContext.GetOrCreateSettingsAsync(guildId, settings => settings.Members);

        foreach (var guildMember in settings.Members)
        {
            if (guildMember.DiscordId == member.Id)
                return Response($"{member} is already verified. We do not do things twice. That is inefficient.");

            if (guildMember.Tags.Contains(userTag, StringComparer.CurrentCultureIgnoreCase))
                return Response($"Identity theft is not a joke, {Context.Author.Mention}");
        }

        var clanMembers = await clashApiClient.GetClanMembersAsync(settings.ClanTag!, Context.CancellationToken);
        if (clanMembers == null)
            return Response("Clan not found. I searched. I am thorough. It is not there.");

        var clanMember = clanMembers.FirstOrDefault(member => member.Tag.Equals(userTag, StringComparison.CurrentCultureIgnoreCase));

        if (clanMember == null)
            return Response($"{userTag} is not a member of this clan. I do not vouch for strangers.");

        var newMember = new ClashMember(guildId, member.Id, new[] { userTag }, 0, clanMember.Role, false);

        settings.Members.Add(newMember);

        dbContext.Update(settings);
        await dbContext.SaveChangesAsync();

        await member.ModifyAsync(props => props.Nick = clanMember.Name);

        var guild = Context.Bot.GetGuild(guildId);
        if (guild == null)
            return Response("This guild is not in my cache. A serious breach. Contact your local bot admin.");

        if (guild.Roles.TryGetValue(settings.UnverifiedRoleId, out var unverifiedRole))
            await member.RevokeRoleAsync(unverifiedRole.Id);

        if (guild.Roles.TryGetValue(settings.VerifiedRoleId, out var verifiedRole))
            await member.GrantRoleAsync(verifiedRole.Id);

        if (guild.GetChannel(settings.GeneralChannelId) is ITextChannel generalChannel)
            await generalChannel.SendMessageAsync(new()
            {
                Content = $"""
                           {member.Mention} welcome to Hotel {guild.Name}. Check-in time is now. Check-out time is never.
                           You better learn the rules. If you don't, you'll be eaten in your sleep

                           To get reminders to attack in war execute the /reminders command
                           """
            });

        var roleId = clanMember.Role switch
        {
            ClanRole.CoLeader => settings.CoLeaderRoleId,
            ClanRole.Admin => settings.ElderRoleId,
            _ => 0UL
        };

        if (guild.Roles.ContainsKey(roleId))
            await member.GrantRoleAsync(roleId);

        return Response("Member verified. Their identity is genuine. I have seen to it personally.");
    }

    [SlashCommand("unverified")]
    [Description("Lists everyone in the guild who has not yet proven who they are")]
    public async ValueTask<IResult> UnverifiedAsync()
    {
        var guildId = Context.GuildId;
        var guild = Context.Bot.GetGuild(guildId);
        if (guild == null)
            return Response("This guild is not in my cache. A serious breach. Contact your local bot admin.");

        var settings = await dbContext.GetOrCreateSettingsAsync(guildId);

        if (!guild.Roles.TryGetValue(settings.UnverifiedRoleId, out var role))
            return Response("There is no unverified role configured. You cannot police what you have not defined. Set it up.");

        var unverifiedMembers = guild.Members.Values
            .Where(member => member.RoleIds.Contains(role.Id))
            .Select(member => member.Nick ?? member.Name);

        return Response($"The following have not proven their identities. I am watching them:\n{string.Join('\n', unverifiedMembers)}");
    }

    [SlashCommand("trigger-welcome")]
    [Description("Summons the welcome message again, by hand, because I said so")]
    [RequireAuthorPermissions(Permissions.ManageRoles)]
    public async ValueTask<IResult> TriggerWelcomeModal([Description("The member to summon the welcome for")] IMember member)
    {
        var settings = await dbContext.GetOrCreateSettingsAsync(Context.GuildId);
        if (settings.Password is null)
            return Response("There is no password configured. Security cannot function without a password. Set one.");

        var view = new WelcomeView(
            Context.Bot.GetGuild(Context.GuildId)!.Name,
            townhallConfiguration.Value.BaseLinkByLevel,
            member.Id,
            settings.Password
        );

        return View(view);
    }
}
