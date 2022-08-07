﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ClashWrapper;
using ClashWrapper.Entities.War;
using ClashWrapper.Models.League;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dwight;

public class WarReminderService : DiscordBotService
{
    private readonly PollingConfiguration _pollingConfiguration;
    private readonly ClashClient _clashClient;
    private readonly HttpClient _httpClient;

    public WarReminderService(IOptions<PollingConfiguration> pollingConfiguration, ClashClient clashClient, HttpClient httpClient)
    {
        _pollingConfiguration = pollingConfiguration.Value;
        _clashClient = clashClient;
        _httpClient = httpClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_pollingConfiguration.WarReminderEnabled)
            return;

        await Bot.WaitUntilReadyAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckForWarsAsync(stoppingToken);
            await Task.Delay(_pollingConfiguration.WarReminderPollingDuration, stoppingToken);
        }
    }

    private async Task CheckForWarsAsync(CancellationToken cancellationToken)
    {
        await using var scope = Bot.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetDwightDbContext();

        var save = false;
        var allReminders = context.GuildSettings.Include(settings => settings.CurrentWarReminder);
        await foreach (var settings in allReminders.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            var guildId = settings.GuildId;
            var clanTag = settings.ClanTag;
            if (clanTag == null)
            {
                Logger.LogInformation("Clan tag not set for {Guild}", guildId);
                continue;
            }

            var currentWar = await GetCurrentWarAsync(settings);
            if (currentWar == null)
                continue;

            var currentWarData = GetCurrentWarData(currentWar, clanTag);
            if (currentWarData == null)
            {
                Logger.LogError("Failed to get current war data for {ClanTag}", clanTag);
                continue;
            }

            var opponentTag = currentWarData.Opponent.Tag;
            var savedWar = settings.CurrentWarReminder;
            var currentReminder = savedWar != null && savedWar.EnemyClan != opponentTag
                ? savedWar
                : new(guildId, opponentTag);

            var channel = Bot.GetChannel(guildId, settings.WarChannelId)
                ?? await Bot.FetchChannelAsync(settings.WarChannelId, cancellationToken: cancellationToken);

            if (channel is not IMessageChannel warChannel)
            {
                Logger.LogInformation("{Guild} has not set their war channel, or it is not a text channel", guildId);
                continue;
            }

            var warRole = Bot.GetRole(guildId, settings.WarRoleId);
            if (warRole == null)
                Logger.LogInformation("{Guild} hasn't set their war role", guildId);

            switch (currentWarData.State)
            {
                case WarState.Preparation when !currentReminder.DeclaredPosted:
                {
                    // todo add role
                    var response = await _httpClient.GetAsync($"https://points.fwa.farm/result.php?clan={clanTag.Replace("#", "")}", cancellationToken);
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    if (!response.IsSuccessStatusCode)
                    {
                        Logger.LogError(new Exception(body), "There was a problem retrieving html from fwa");
                        continue;
                    }

                    var document = new HtmlDocument();
                    document.LoadHtml(body);
                    var outcomeNode = document.DocumentNode.SelectSingleNode("/html/body/main/div/center[2]/span");

                    var message = new LocalMessage
                    {
                        Content = $"War has been declared!\n{outcomeNode}"
                    };
                    await warChannel.SendMessageAsync(message, cancellationToken: cancellationToken);
                    currentReminder.DeclaredPosted = true;
                    save = true;
                    break;
                }

                case WarState.InWar:
                {
                    if (!currentReminder.StartedPosted)
                    {
                        var message = new LocalMessage
                        {
                            Content = warRole == null ? "War has started!" : $"{warRole.Mention}, war has started!"
                        };
                        await warChannel.SendMessageAsync(message, cancellationToken: cancellationToken);
                        currentReminder.StartedPosted = true;
                        save = true;
                    }
                    else if (!currentReminder.ReminderPosted && currentWarData.EndTime - DateTimeOffset.UtcNow < TimeSpan.FromHours(1))
                    {
                        var missedAttacks = currentWarData.Clan.Members.Where(member => member.Attacks.Count < 2).ToList();
                        if (missedAttacks.Count == 0)
                            continue;

                        var inWarTags = currentWarData.Clan.Members.Select(member => member.Tag).ToHashSet();
                        var clashMembers = await context.Members.Where(member => member.GuildId == settings.GuildId).ToListAsync(cancellationToken);
                        var inDiscord = clashMembers.Where(member => member.Tags.Any(tag => inWarTags.Contains(tag)));
                        var mentions = string.Join("\n", inDiscord.Select(member => Mention.User(member.DiscordId)));

                        var message = new LocalMessage
                        {
                            Content = $"War ends soon,\n{mentions}\n\nYou still need to attack!"
                        };
                        await warChannel.SendMessageAsync(message, cancellationToken: cancellationToken);

                        currentReminder.ReminderPosted = true;
                        save = true;
                    }

                    break;
                }
            }
        }

        if (save)
            await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<CurrentWar?> GetCurrentWarAsync(GuildSettings settings)
    {
        var clanTag = settings.ClanTag!;
        var currentWar = await _clashClient.GetCurrentWarAsync(clanTag!);
        if (currentWar.State == WarState.InWar)
            return currentWar;

        Logger.LogInformation("{ClanTag} is not currently in a normal war, checking for CWL", clanTag);

        var group = await _clashClient.GetLeagueGroupAsync(clanTag);
        if (group.State == State.InWar)
            return await GetCurrentWarAsync(group.Rounds, 0, 0, clanTag);
        
        Logger.LogInformation("{ClanTag} is not currently in CWL", clanTag);
        return null;
    }

    private async Task<CurrentWar?> GetCurrentWarAsync(List<Round> rounds, int round, int tag, string clanTag)
    {
        // thanks clash api
        while (round != -1 && tag != 4)
        {
            var currentRound = rounds[round--];
            var currentTag = currentRound.WarTags[tag++];

            var currentWar = await _clashClient.GetLeagueWarAsync(currentTag);
            if (currentWar.State != WarState.InWar)
                continue;

            return currentWar;
        }

        return null;
    }

    private static CurrentWarData? GetCurrentWarData(CurrentWar currentWar, string clanTag)
    {
        // thanks clash api
        if (currentWar.Clan.Tag == clanTag)
            return new(currentWar.State, currentWar.EndTime, currentWar.Clan, currentWar.Opponent);

        if (currentWar.Opponent.Tag == clanTag)
            return new(currentWar.State,currentWar.EndTime, currentWar.Opponent, currentWar.Clan);

        return null;
    }

    private record CurrentWarData(WarState State, DateTimeOffset EndTime, WarClan Clan, WarClan Opponent);
}