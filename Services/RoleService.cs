using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disqord.Bot.Hosting;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dwight;

public class RoleService : DiscordBotService
{
    private readonly PollingConfiguration _pollingConfiguration;
    private readonly ClashApiClient _clashApiClient;

    public RoleService(IOptions<PollingConfiguration> pollingConfiguration, ClashApiClient clashApiClient)
    {
        _clashApiClient = clashApiClient;
        _pollingConfiguration = pollingConfiguration.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_pollingConfiguration.RoleCheckingEnabled)
            return;

        await Bot.WaitUntilReadyAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckRolesAsync(stoppingToken);
            await Task.Delay(_pollingConfiguration.WarReminderPollingDuration, stoppingToken);
        }
    }

    private async Task CheckRolesAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Checking roles");

        await using var scope = Bot.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetDwightDbContext();

        var allSettings = await context.GuildSettings
            .Include(settings => settings.Members)
            .ToListAsync(cancellationToken);

        var save = false;
        foreach (var settings in allSettings)
        {
            Logger.LogDebug("Checking roles for guild {GuildId}", settings.GuildId);

            if (settings.ClanTag == null)
            {
                Logger.LogDebug("Clan tag not set for {GuildId}, skipping", settings.GuildId);
                continue;
            }

            var clanMembers = await _clashApiClient.GetClanMembersAsync(settings.ClanTag, cancellationToken);
            if (clanMembers == null || clanMembers.Count == 0)
            {
                Logger.LogDebug("Got no members for clan {ClanTag}", settings.ClanTag);
                continue;
            }

            var discordMembers = (await Bot.FetchMembersAsync(settings.GuildId, cancellationToken: cancellationToken)).ToDictionary(member => member.Id);

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

                    await Bot.GrantRoleAsync(settings.GuildId, clashMember.DiscordId, roleId, cancellationToken: cancellationToken);
                }

                Logger.LogDebug("Checking roles for member {MemberId}", clashMember.DiscordId);

                // todo yeet
                async Task UpdateRoleAsync(GuildSettings guildSettings, ClashMember clashMember, ClanRole role, ulong removeId, ulong addId)
                {
                    Logger.LogInformation("Giving member {MemberId} role {RoleId}", clashMember.DiscordId, addId);

                    await Bot.RevokeRoleAsync(guildSettings.GuildId, clashMember.DiscordId, removeId, cancellationToken: cancellationToken);
                    await Bot.GrantRoleAsync(guildSettings.GuildId, clashMember.DiscordId, addId, cancellationToken: cancellationToken);
                    clashMember.Role = role;
                }

                var beforeRole = clashMember.Role;
                switch (clanMember.Role)
                {
                    case ClanRole.Admin:
                        await UpdateRoleAsync(settings, clashMember, ClanRole.Admin, settings.CoLeaderRoleId, settings.ElderRoleId);
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
            await context.SaveChangesAsync(cancellationToken);
    }

    private static ulong GetRoleId(GuildSettings settings, ClanRole role)
    {
        return role switch
        {
            ClanRole.Admin => settings.ElderRoleId,
            ClanRole.Leader => settings.CoLeaderRoleId,
            ClanRole.CoLeader => settings.CoLeaderRoleId
        };
    }
}