using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClashWrapper;
using ClashWrapper.Entities.ClanMembers;
using Disqord.Bot.Hosting;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dwight.Services;

public class RoleService : DiscordBotService
{
    private readonly EspeonScheduler _scheduler;
    private readonly PollingConfiguration _pollingConfiguration;
    private readonly ClashClient _clashClient;

    public RoleService(EspeonScheduler scheduler, IOptions<PollingConfiguration> pollingConfiguration, ClashClient clashClient)
    {
        _scheduler = scheduler;
        _clashClient = clashClient;
        _pollingConfiguration = pollingConfiguration.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_pollingConfiguration.RoleCheckingEnabled)
            return;
            
        await Bot.WaitUntilReadyAsync(stoppingToken);

        _scheduler.DoNow(CheckRolesAsync);
    }

    private async Task CheckRolesAsync()
    {
        Logger.LogInformation("Checking roles");
            
        await using var scope = Bot.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetDwightDbContext();

        var allSettings = await context.GuildSettings
            .Include(settings => settings.Members)
            .ToListAsync();

        var save = false;
        foreach (var settings in allSettings)
        {
            Logger.LogDebug("Checking roles for guild {GuildId}", settings.GuildId);

            if (settings.ClanTag == null)
            {
                Logger.LogDebug("Clan tag not set for {GuildId}, skipping", settings.GuildId);
                continue;
            }

            var clanMembers = await _clashClient.GetClanMembersAsync(settings.ClanTag);
            if (clanMembers.Count == 0)
            {
                Logger.LogDebug("Got no members for clan {ClanTag}", settings.ClanTag);
                continue;
            }

            var discordMembers = (await Bot.FetchMembersAsync(settings.GuildId)).ToDictionary(member => member.Id);

            foreach (var clanMember in clanMembers)
            {
                var clashMember = settings.Members.FirstOrDefault(member => member.Tags.Contains(clanMember.Tag, StringComparer.CurrentCultureIgnoreCase));
                if (clashMember == null)
                    continue;

                Logger.LogDebug("clanMember role {One}, clashMember role {Two}", clanMember.Role, clashMember?.Role);

                var discordMember = discordMembers[clashMember!.DiscordId];

                if (clashMember.Role == clanMember.Role)
                {
                    var roleId = GetRoleId(settings, clashMember.Role);
                    
                    if (discordMember.RoleIds.Contains(roleId))
                        continue;

                    Logger.LogInformation("{Member} was missing role {Role}", discordMember.Id, roleId);

                    await Bot.GrantRoleAsync(settings.GuildId, clashMember.DiscordId, roleId);
                }

                Logger.LogDebug("Checking roles for member {MemberId}", clashMember.DiscordId);

                // todo yeet
                async Task UpdateRoleAsync(GuildSettings guildSettings, ClashMember clashMember, ClanRole role, ulong removeId, ulong addId)
                {
                    Logger.LogInformation("Giving member {MemberId} role {RoleId}", clashMember.DiscordId, addId);

                    await Bot.RevokeRoleAsync(guildSettings.GuildId, clashMember.DiscordId, removeId);
                    await Bot.GrantRoleAsync(guildSettings.GuildId, clashMember.DiscordId, addId);
                    clashMember.Role = role;
                }

                var beforeRole = clashMember.Role;
                switch (clanMember.Role)
                {
                    case ClanRole.Elder:
                        await UpdateRoleAsync(settings, clashMember, ClanRole.Elder, settings.CoLeaderRoleId, settings.ElderRoleId);
                        break;

                    case ClanRole.Leader:
                    case ClanRole.CoLeader:
                        await UpdateRoleAsync(settings, clashMember, ClanRole.CoLeader, settings.ElderRoleId, settings.CoLeaderRoleId);
                        break;
                }

                save |= beforeRole != clashMember.Role;
            }
        }

        if (save)
            await context.SaveChangesAsync();

        _scheduler.DoIn(_pollingConfiguration.RoleCheckingPollingDuration, CheckRolesAsync);
    }

    private static ulong GetRoleId(GuildSettings settings, ClanRole role)
    {
        return role switch
        {
            ClanRole.Elder => settings.ElderRoleId,
            ClanRole.Leader => settings.CoLeaderRoleId,
            ClanRole.CoLeader => settings.CoLeaderRoleId
        };
    }
}