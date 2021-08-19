using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Rest;
using Qmmands;

namespace Dwight
{
    [RequireAuthorGuildPermissions(Permission.ManageRoles)]
    [Group("rep")]
    public class RepModule : DiscordGuildModuleBase
    {
        private readonly DwightDbContext _dbContext;

        public RepModule(DwightDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [Command("add")]
        public async ValueTask<CommandResult> AddRepAsync(IMember member, float timezone)
        {
            var rep = new FwaRep(Context.GuildId, member.Id, timezone);
            await _dbContext.FwaReps.AddAsync(rep);
            await _dbContext.SaveChangesAsync();

            var settings = await _dbContext.GetOrCreateSettingsAsync(Context.GuildId);
            if (Context.Guild.Roles.TryGetValue(settings.RepRoleId, out var repRole))
                await member.GrantRoleAsync(repRole.Id);

            return Reply($"Added {member.Mention} as a rep");
        }

        [Command("remove")]
        public ValueTask<CommandResult> RemoveRepAsync(IMember member)
            => RemoveRepAsync(member.Id);
        
        [Command("remove")]
        public ValueTask<CommandResult> RemoveRepAsync(Snowflake id)
        {
            return Reply("implement plz");
        } 
    }
}