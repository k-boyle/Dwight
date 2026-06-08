using System;
using System.Collections.Generic;
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

/// <summary>
/// Polls each guild's clan roster and posts a cc.fwafarm link to the configured channel whenever a
/// player Dwight has never seen before turns up. Seen tags are remembered forever, so a member who
/// leaves and rejoins is not re-announced. On the first observation of a clan the roster is seeded
/// silently to avoid spamming a link for every existing member.
/// </summary>
public class NewMemberService : DiscordBotService
{
    private readonly PollingConfiguration _pollingConfiguration;
    private readonly ClashApiClient _clashApiClient;

    public NewMemberService(IOptions<PollingConfiguration> pollingConfiguration, ClashApiClient clashApiClient)
    {
        _clashApiClient = clashApiClient;
        _pollingConfiguration = pollingConfiguration.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_pollingConfiguration.NewMemberTrackingEnabled)
            return;

        await Bot.WaitUntilReadyAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckNewMembersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An exception occured whilst checking for new clan members");
            }

            await Task.Delay(_pollingConfiguration.NewMemberPollingDuration, stoppingToken);
        }
    }

    private async Task CheckNewMembersAsync(CancellationToken cancellationToken)
    {
        await using var scope = Bot.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetDwightDbContext();

        var allSettings = await context.GuildSettings.ToListAsync(cancellationToken);

        var save = false;
        foreach (var settings in allSettings)
        {
            if (settings.ClanTag == null)
                continue;

            var clanMembers = await _clashApiClient.GetClanMembersAsync(settings.ClanTag, cancellationToken);
            if (clanMembers == null || clanMembers.Count == 0)
            {
                Logger.LogDebug("Got no members for clan {ClanTag}", settings.ClanTag);
                continue;
            }

            var seenTags = await context.SeenClanMembers
                .Where(member => member.GuildId == settings.GuildId)
                .Select(member => member.Tag)
                .ToListAsync(cancellationToken);
            var seen = new HashSet<string>(seenTags, StringComparer.OrdinalIgnoreCase);

            // First time we've seen this clan — record everyone silently so we don't announce the
            // entire existing roster.
            var seeding = seen.Count == 0;

            foreach (var clanMember in clanMembers)
            {
                if (!seen.Add(clanMember.Tag))
                    continue;

                context.SeenClanMembers.Add(new SeenClanMember(settings.GuildId, clanMember.Tag));
                save = true;

                if (seeding)
                    continue;

                if (settings.NewMemberChannelId == 0)
                    continue;

                Logger.LogInformation("New member {Tag} joined clan {ClanTag}", clanMember.Tag, settings.ClanTag);

                var reply = $"https://cc.fwafarm.com/cc_n/member.php?tag={clanMember.Tag.TrimStart('#')}";
                await Bot.SendMessageAsync(settings.NewMemberChannelId, new() { Content = reply });
            }
        }

        if (save)
            await context.SaveChangesAsync(cancellationToken);
    }
}
