using System;
using System.Linq;
using System.Threading.Tasks;
using ClashWrapper;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Dwight
{
    public class VerificationModule : DiscordGuildModuleBase
    {
        private readonly ClashClient _clashClient;
        private readonly DwightDbContext _dbContext;

        public VerificationModule(ClashClient clashClient, DwightDbContext dbContext)
        {
            _clashClient = clashClient;
            _dbContext = dbContext;
        }

        [Command("verify")]
        [RequireBotGuildPermissions(Permission.ManageRoles)]
        [RequireBotGuildPermissions(Permission.SetNick)]
        [RequireAuthorGuildPermissions(Permission.ManageRoles)]
        public async ValueTask<CommandResult> VerifyAsync(IMember member, string userTag)
        {
            userTag = userTag.ToUpper();
            
            var settings = await _dbContext.GetOrCreateSettingsAsync(Context.GuildId, settings => settings.Members);

            if (settings.Members.Any(clanMember => clanMember.DiscordId == member.Id))
                return Reply($"{member} is already verified");

            if (settings.ClanTag == null)
                return Reply("Clan tag has not been configured for this clan");

            var clanMembers = await _clashClient.GetClanMembersAsync(settings.ClanTag);

            var clanMember = clanMembers.FirstOrDefault(member => member.Tag.Equals(userTag, StringComparison.CurrentCultureIgnoreCase));

            if (clanMember == null)
                return Reply($"{userTag} is not a member of the clan");

            var newMember = new ClashMember(Context.GuildId, member.Id, new[] { userTag }, 0);
            settings.Members.Add(newMember);

            _dbContext.Update(settings);
            await _dbContext.SaveChangesAsync();

            await member.ModifyAsync(props => props.Nick = clanMember.Name);

            if (Context.Guild.Roles.TryGetValue(settings.UnverifiedRoleId, out var urole))
                await member.RevokeRoleAsync(urole.Id);

            if (Context.Guild.Roles.TryGetValue(settings.VerifiedRoleId, out var vrole))
                await member.GrantRoleAsync(vrole.Id);

            if (Context.Guild.GetChannel(settings.GeneralChannelId) is ITextChannel channel)
                await channel.SendMessageAsync(new() { Content = $"{member.Mention} welcome to {Context.Guild}!" });

            return Reply("Member has been verified");
        }

        [Command("unverified")]
        public async ValueTask<CommandResult> UnverifiedAsync()
        {
            var settings = await _dbContext.GetOrCreateSettingsAsync(Context.GuildId);

            if (!Context.Guild.Roles.TryGetValue(settings.UnverifiedRoleId, out var role))
                return Reply("Unverified role has not been setup yet");

            var unverifiedMembers = Context.Guild.Members.Values
                .Where(member => member.RoleIds.Contains(role.Id))
                .Select(member => member.Nick ?? member.Name);

            return Reply($"Unverified members:\n{string.Join('\n', unverifiedMembers)}");
        }

        [Command("addtag")]
        [RequireAuthorGuildPermissions(Permission.ManageRoles)]
        public async ValueTask<CommandResult> AddTagAsync(IMember member, string tag)
        {
            tag = tag.ToUpper();
            
            var settings = await _dbContext.GetOrCreateSettingsAsync(Context.GuildId, settings => settings.Members);

            var clashMembers = await _clashClient.GetClanMembersAsync(settings.ClanTag);
            var clashMember = clashMembers.FirstOrDefault(member => member.Tag.Equals(tag, StringComparison.CurrentCultureIgnoreCase));

            if (clashMember == null)
                return Reply($"A member with tag {tag} does not exist in the clan");

            var alreadyTaken = await _dbContext.Members.FirstOrDefaultAsync(member => member.Tags.Contains(tag));
            if (alreadyTaken != null)
                return Reply($"{tag} already belongs to {(Context.Guild.Members.TryGetValue(alreadyTaken.DiscordId, out member) ? member : "{not in cache}")}");

            var clanMember = await _dbContext.Members.FindAsync(Context.GuildId.RawValue, member.Id.RawValue);
            if (clanMember == null)
                return Reply($"{member} has not been verified");

            if (clanMember.Tags.Contains(tag, StringComparer.CurrentCultureIgnoreCase))
                return Reply($"{member} already has tag {tag}");

            clanMember.Tags = clanMember.Tags.Append(tag).ToArray();
            _dbContext.Update(clanMember);
            await _dbContext.SaveChangesAsync();

            return Reply($"Added tag {tag} to {member}");
        }
        
        [Command("removetag")]
        [RequireAuthorGuildPermissions(Permission.ManageRoles)]
        public async ValueTask<CommandResult> RemoveTagAsync(IMember member, string tag)
        {
            tag = tag.ToUpper();
            
            var clanMember = await _dbContext.Members.FindAsync(Context.GuildId.RawValue, member.Id.RawValue);
            if (clanMember == null)
                return Reply($"{member} has not been verified");

            if (clanMember.Tags.Length == 1)
                return Reply($"{tag} is the last tag that {member} has, cannot remove it");
            
            if (!clanMember.Tags.Contains(tag, StringComparer.CurrentCultureIgnoreCase))
                return Reply($"{member} doesn't have tag {tag}");

            clanMember.Tags = clanMember.Tags.Where(other => !other.Equals(tag, StringComparison.CurrentCultureIgnoreCase)).ToArray();
            _dbContext.Update(clanMember);
            await _dbContext.SaveChangesAsync();

            return Reply($"Removed tag {tag} to {member}");
        }
    }
}