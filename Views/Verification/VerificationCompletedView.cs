using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.Extensions.DependencyInjection;

namespace Dwight;

public class VerificationCompletedView(Action<LocalMessageBase> messageTemplate, Snowflake userId, string tag) 
    : ViewBase(messageTemplate)
{
    [Button(Emoji = "⚔️", Label = "Accept")]
    public async ValueTask AcceptButtonAsync(ButtonEventArgs e)
    {
        if (!e.Interaction.ApplicationPermissions.HasFlag(Permissions.ManageRoles & Permissions.SetNick))
        {
            var appFailedPermissions = new LocalInteractionMessageResponse()
                .WithContent($"{Mention.User(e.Interaction.ApplicationId)} is missing manage roles or set nickname permissions")
                .WithIsEphemeral();
            await e.Interaction.Response().SendMessageAsync(appFailedPermissions);
            return;
        }

        if (!e.Interaction.AuthorPermissions.HasFlag(Permissions.ManageRoles))
        {
            var userFailedPermissions = new LocalInteractionMessageResponse()
                .WithContent($"{Mention.User(e.Interaction.AuthorId)} you are missing the manage roles permission")
                .WithIsEphemeral();
            await e.Interaction.Response().SendMessageAsync(userFailedPermissions);
            return;
        }

        var bot = e.Interaction.Client as DiscordBot;
        var guildId = e.Interaction.GuildId!.Value;
        var member = e.Interaction.Author as IMember;

        await using var scope = bot!.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetDwightDbContext();
        
        var settings = await dbContext.GetOrCreateSettingsAsync(guildId, settings => settings.Members);
        
        foreach (var guildMember in settings.Members)
        {
            if (guildMember.DiscordId == userId) {
                ClearComponents();

                var alreadyVerified = new LocalInteractionMessageResponse()
                    .WithContent($"{Mention.User(userId)} is already verified")
                    .WithIsEphemeral();
                await e.Interaction.Response().SendMessageAsync(alreadyVerified);
                return;
            }

            if (guildMember.Tags.Contains(tag, StringComparer.CurrentCultureIgnoreCase))
            {
                ClearComponents();

                var identityTheft = new LocalInteractionMessageResponse()
                    .WithContent($"Identity theft is not a joke, {e.Interaction.Author.Mention}")
                    .WithIsEphemeral();
                await e.Interaction.Response().SendMessageAsync(identityTheft);
                return;
            }
        }

        var clashClient = scope.ServiceProvider.GetRequiredService<ClashApiClient>();
        
        var clanMembers = await clashClient.GetClanMembersAsync(settings.ClanTag!, CancellationToken.None);
        if (clanMembers == null)
        {
            ClearComponents();
            
            var clanNotFound = new LocalInteractionMessageResponse()
                .WithContent($"Clan {settings.ClanTag!} not found")
                .WithIsEphemeral();
            await e.Interaction.Response().SendMessageAsync(clanNotFound);
            return;
        }

        var clanMember = clanMembers.FirstOrDefault(member => member.Tag.Equals(tag, StringComparison.CurrentCultureIgnoreCase));

        if (clanMember == null)
        {
            var notInClan = new LocalInteractionMessageResponse()
                .WithContent($"{Mention.User(userId)} is not in the clan")
                .WithIsEphemeral();
            await e.Interaction.Response().SendMessageAsync(notInClan);
            return;
        }
        
        ClearComponents();
        
        var newMember = new ClashMember(guildId, member!.Id, new[] { tag }, 0, clanMember.Role, false);
        
        settings.Members.Add(newMember);

        dbContext.Update(settings);
        await dbContext.SaveChangesAsync();
        
        await member.ModifyAsync(props => props.Nick = clanMember.Name);
        
        var guild = bot.GetGuild(guildId);
        if (guild == null)
        {
            var notInCache = new LocalInteractionMessageResponse()
                .WithContent($"{guildId} is not in the bot cache, contact your local bot admin")
                .WithIsEphemeral();
            await e.Interaction.Response().SendMessageAsync(notInCache);
            return;
        }
        
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

        var accepted = new LocalInteractionMessageResponse()
            .WithContent($"{Mention.User(userId)} has been accepted");
        
        await e.Interaction.Response().SendMessageAsync(accepted);
    }
}
