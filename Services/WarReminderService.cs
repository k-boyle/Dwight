using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
    private readonly ClashApiClient _clashApiClient;
    private readonly HttpClient _httpClient;

    public WarReminderService(IOptions<PollingConfiguration> pollingConfiguration, ClashApiClient clashApiClient, HttpClient httpClient)
    {
        _pollingConfiguration = pollingConfiguration.Value;
        _clashApiClient = clashApiClient;
        _httpClient = httpClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_pollingConfiguration.WarReminderEnabled)
            return;

        await Bot.WaitUntilReadyAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckForWarsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An exception occured whilst checking for wars");
            }

            await Task.Delay(_pollingConfiguration.WarReminderPollingDuration, stoppingToken);
        }
    }

    private async Task CheckForWarsAsync(CancellationToken cancellationToken)
    {
        await using var scope = Bot.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetDwightDbContext();

        var save = false;
        var allSettings = await context.GuildSettings.Include(settings => settings.CurrentWarReminder)
            .Include(settings => settings.Members)
            .ToListAsync(cancellationToken);
        // todo this _should_ really be parallel
        foreach (var settings in allSettings)
        {
            var guildId = settings.GuildId;
            var clanTag = settings.ClanTag;
            if (clanTag == null)
            {
                Logger.LogInformation("Clan tag not set for {Guild}", guildId);
                continue;
            }

            var currentWar = await GetCurrentWarAsync(settings, cancellationToken);
            if (currentWar == null)
            {
                Logger.LogInformation("{ClanTag} is currently not in war", clanTag);
                continue;
            }

            var opponentTag = currentWar.Opponent.Tag;
            var savedWar = settings.CurrentWarReminder;
            var currentReminder = savedWar != null && savedWar.EnemyClan == opponentTag
                ? savedWar
                : new(guildId, opponentTag);

            settings.CurrentWarReminder = currentReminder;

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

            switch (currentWar.State)
            {
                case WarState.Preparation when !currentReminder.DeclaredPosted:
                {
                    // todo add role
                    var resultUri = $"https://points.fwa.farm/result.php?clan={clanTag.Replace("#", "")}";
                    var response = await _httpClient.GetAsync(resultUri, cancellationToken);
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    if (!response.IsSuccessStatusCode)
                    {
                        Logger.LogError(new Exception(body), "There was a problem retrieving html from fwa");
                        continue;
                    }

                    var document = new HtmlDocument();
                    document.LoadHtml(body);
                    var outcomeRegex = new Regex(@"\w[\w\s]+", RegexOptions.Compiled | RegexOptions.RightToLeft);
                    var outcomeNode = document.DocumentNode.SelectSingleNode("/html/body/main/div/center[3]/span");
                    var match = outcomeRegex.Match(outcomeNode.InnerText);

                    if (!match.Success)
                    {
                        Logger.LogError("Failed to scrape fwa website");
                        continue;
                    }

                    var message = new LocalMessage
                    {
                        Content = $"War has been declared!\n{match.Value}\nGo to {resultUri} to see the breakdown"
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
                        Logger.LogInformation("Posting war started for {ClanTag}", clanTag);

                        var message = new LocalMessage
                        {
                            Content = warRole == null
                                ? $"War has started against {currentWar.Opponent.Name}!"
                                : $"{warRole.Mention}, war has started against {currentWar.Opponent.Name}!"
                        };
                        await warChannel.SendMessageAsync(message, cancellationToken: cancellationToken);
                        currentReminder.StartedPosted = true;
                        save = true;
                    }
                    else
                    {
                        var endsIn = currentWar.EndTime - DateTimeOffset.UtcNow;
                        switch (currentWar.Cwl)
                        {
                            case true when endsIn < TimeSpan.FromHours(4) && currentReminder.CwlRemindersPosted < 4 && (currentReminder.CwlRemindersPosted == 0 || currentReminder.CwlReminderLastPosted != endsIn.Hours):
                            {
                                Logger.LogInformation("Posting cwl attack reminders for {ClanTag}", clanTag);

                                var missedAttacks = currentWar.Clan.Members.Where(member => member.Attacks == null)
                                    .Select(member => member.Tag)
                                    .ToHashSet();

                                if (missedAttacks.Count == 0)
                                    continue;

                                var inDiscord = settings.Members.Where(member => member.Tags.Any(tag => missedAttacks.Contains(tag)));
                                var mentions = string.Join(", ", inDiscord.Select(member => Mention.User(member.DiscordId)));

                                var message = new LocalMessage
                                {
                                    Content = $"War ends in {endsIn.Hours + 1} hour(s),\n{mentions}\n\nYou still need to attack!"
                                };
                                await warChannel.SendMessageAsync(message, cancellationToken: cancellationToken);

                                currentReminder.CwlRemindersPosted++;
                                currentReminder.CwlReminderLastPosted = endsIn.Hours;
                                save = true;
                                break;
                            }
                            case false when !currentReminder.ReminderPosted && endsIn < TimeSpan.FromHours(1):
                            {
                                Logger.LogInformation("Posting attack reminders for {ClanTag}", clanTag);

                                var missedAttacks = currentWar.Clan.Members.Where(member => member.Attacks is null or { Length: < 2 })
                                    .Where(member => Remind(settings, member.Tag))
                                    .Select(member => member.Tag)
                                    .ToHashSet();

                                if (missedAttacks.Count == 0)
                                {
                                    Logger.LogInformation("No missed attacks for {ClanTag}", clanTag);
                                    continue;
                                }

                                var inDiscord = settings.Members.Where(member => member.Tags.Any(tag => missedAttacks.Contains(tag)));
                                var mentions = string.Join(", ", inDiscord.Select(member => Mention.User(member.DiscordId)));

                                var message = new LocalMessage
                                {
                                    Content = $"War ends soon! \n{mentions}\n\nYou still need to attack!"
                                };
                                await warChannel.SendMessageAsync(message, cancellationToken: cancellationToken);

                                currentReminder.ReminderPosted = true;
                                save = true;
                                break;
                            }

                            default:
                                Logger.LogInformation("Nothing to post for {ClanTag}", clanTag);
                                break;
                        }
                    }

                    break;
                }
            }
        }

        if (save)
            await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<CurrentWarData?> GetCurrentWarAsync(GuildSettings settings, CancellationToken cancellationToken)
    {
        var clanTag = settings.ClanTag!;
        var currentWar = await _clashApiClient.GetCurrentWarAsync(clanTag!, cancellationToken);
        if (currentWar is { State: WarState.InWar or WarState.Preparation })
            return new(currentWar.State, currentWar.EndTime, currentWar.Clan, currentWar.Opponent, false);

        Logger.LogInformation("{ClanTag} is not currently in a normal war, checking for CWL", clanTag);

        var group = await _clashApiClient.GetLeagueGroupAsync(clanTag, cancellationToken);
        if (group == null)
            return null;

        if (group.State == LeagueState.InWar)
            return await GetCurrentWarAsync(group.Rounds, group.Rounds.Length - 1, 0, clanTag, cancellationToken);

        Logger.LogInformation("{ClanTag} is not currently in CWL", clanTag);
        return null;
    }

    private async Task<CurrentWarData?> GetCurrentWarAsync(LeagueRound[] rounds, int round, int tag, string clanTag, CancellationToken cancellationToken)
    {
        // thanks clash api
        while (round != -1 && tag != 4)
        {
            Logger.LogInformation("Looking for CWL for {ClanTag}, {Round}[{Tag}]", clanTag, round, tag);

            var currentRound = rounds[round];
            var currentTag = currentRound.WarTags[tag];

            var currentWar = await _clashApiClient.GetLeagueWarAsync(currentTag, cancellationToken);

            if (currentWar == null)
            {
                Logger.LogError("Unexpected null war for {ClanTag}", clanTag);
                return null;
            }
            
            if (currentWar.State != WarState.InWar)
            {
                round--;
                tag = 0;
                continue;
            }

            var currentWarData = GetCurrentCwlWarData(currentWar, clanTag);
            if (currentWarData != null)
            {
                Logger.LogInformation("Found CWL for {ClanTag}", clanTag);
                return currentWarData;
            }

            tag++;
        }

        return null;
    }

    private static CurrentWarData? GetCurrentCwlWarData(CurrentWar currentWar, string clanTag)
    {
        // thanks clash api
        if (currentWar.Clan.Tag == clanTag)
            return new(currentWar.State, currentWar.EndTime, currentWar.Clan, currentWar.Opponent, true);

        if (currentWar.Opponent.Tag == clanTag)
            return new(currentWar.State, currentWar.EndTime, currentWar.Opponent, currentWar.Clan, true);

        return null;
    }

    private record CurrentWarData(WarState State, DateTimeOffset EndTime, WarClan Clan, WarClan Opponent, bool Cwl);

    private static bool Remind(GuildSettings settings, string tag)
    {
        return settings.Members.FirstOrDefault(member => Array.FindIndex(member.Tags, str => str == tag) != -1)?.Remind ?? false;
    }
}