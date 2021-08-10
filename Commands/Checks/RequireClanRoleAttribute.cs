using System.Linq;
using System.Threading.Tasks;
using Disqord.Bot;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Dwight
{
    public class RequireClanRoleAttribute : DiscordGuildCheckAttribute
    {
        public enum ClanRole
        {
            FWA_REP,
            CO_LEADER,
            ELDER
        }

        private readonly ClanRole _role;

        public RequireClanRoleAttribute(ClanRole role)
        {
            _role = role;
        }

        public override async ValueTask<CheckResult> CheckAsync(DiscordGuildCommandContext context)
        {
            await using var scope = context.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetDwightDbContext();
            var settings = await dbContext.GetOrCreateSettingsAsync(context.GuildId);

            var roleId = _role switch
            {
                ClanRole.FWA_REP => settings.RepRoleId,
                ClanRole.CO_LEADER => settings.CoLeaderRoleId,
                ClanRole.ELDER => settings.ElderRoleId
            };

            return context.Author.RoleIds.Contains(roleId)
                ? CheckResult.Successful
                : Failure($"You need to be a {_role} to execute this command");
        }
    }
}