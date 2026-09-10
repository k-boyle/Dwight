using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
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
///
/// Each new member's ChocolateClash record is also checked: an outright FWA ban raises an urgent
/// alarm, while a recent stint in an FWA-blacklisted clan raises a milder heads-up.
/// </summary>
public class NewMemberService : DiscordBotService
{
    private readonly PollingConfiguration _pollingConfiguration;
    private readonly ClashApiClient _clashApiClient;
    private readonly FwaMemberClient _fwaMemberClient;

    public NewMemberService(IOptions<PollingConfiguration> pollingConfiguration, ClashApiClient clashApiClient, FwaMemberClient fwaMemberClient)
    {
        _clashApiClient = clashApiClient;
        _fwaMemberClient = fwaMemberClient;
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
            if (!settings.TryGetClanTag(out var clanTag))
                continue;

            var clanMembers = await _clashApiClient.GetClanMembersAsync(clanTag, cancellationToken);
            if (clanMembers == null || clanMembers.Count == 0)
            {
                Logger.LogDebug("Got no members for clan {ClanTag}", clanTag);
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

                Logger.LogInformation("New member {Tag} joined clan {ClanTag}", clanMember.Tag, clanTag);

                var bareTag = clanMember.Tag.TrimStart('#');
                var placeholder = await Bot.SendMessageAsync(settings.NewMemberChannelId,
                    new() { Content = $"A new face has appeared in the clan: {clanMember.Name}. Give me a moment, I am pulling their file." });

                var reply = $"A new face has appeared in the clan: {clanMember.Name}. I have already pulled their file.\nhttps://cc.fwafarm.com/cc_n/member.php?tag={bareTag}";

                var status = await GetFwaStatusAsync(bareTag, cancellationToken);
                if (status is { IsBanned: true })
                {
                    reply += $"\n\n🚨 {Markdown.Bold("THIS ACCOUNT IS FWA BANNED.")} Kick {clanMember.Name} immediately.";
                }
                else if (status?.BlacklistedClan is { } blacklistedClan)
                {
                    reply += $"\n\nHeads up: their ChocolateClash history shows time in {Markdown.Bold(blacklistedClan.ClanName)} " +
                        $"(https://cc.fwafarm.com/cc_n/clan.php?tag={blacklistedClan.ClanTag}), which is FWA blacklisted. Might be worth keeping an eye on them.";
                }

                await Bot.ModifyMessageAsync(settings.NewMemberChannelId, placeholder.Id, props => props.Content = reply, cancellationToken: cancellationToken);
            }
        }

        if (save)
            await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<FwaMemberStatus?> GetFwaStatusAsync(string playerTag, CancellationToken cancellationToken)
    {
        try
        {
            return await _fwaMemberClient.GetStatusAsync(playerTag, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to fetch FWA member status for {PlayerTag}", playerTag);
            return null;
        }
    }
}
