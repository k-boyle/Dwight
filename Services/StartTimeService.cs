using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClashWrapper;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;
using Ical.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dwight.Services
{
    public class StartTimeService : DiscordBotService
    {
        private static readonly TimeSpan TWO_HOURS = TimeSpan.FromHours(2);
        private static readonly TimeSpan TWENTY_FIVE_HOURS = TimeSpan.FromHours(25);

        private readonly ClashClient _clashClient;
        private readonly EspeonScheduler _espeonScheduler;
        private readonly HttpClient _httpClient;
        private readonly PollingConfiguration _pollingConfiguration;
        private readonly Dictionary<ulong, string> _lastEventIdByGuildId;
        private readonly Dictionary<ulong, ulong> _startTimeMessageIdByGuildId;

        public StartTimeService(ClashClient clashClient, EspeonScheduler espeonScheduler, HttpClient httpClient, IOptions<PollingConfiguration> pollingConfiguration)
        {
            _clashClient = clashClient;
            _espeonScheduler = espeonScheduler;
            _httpClient = httpClient;
            _pollingConfiguration = pollingConfiguration.Value;
            _lastEventIdByGuildId = new();
            _startTimeMessageIdByGuildId = new();
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (!_pollingConfiguration.StartTimeEnabled)
                return;

            async Task ExecuteStartTimeRemindersAsync(CancellationToken cancellationToken)
            {
                await using var scope = Bot.Services.CreateAsyncScope();
                var context = scope.ServiceProvider.GetDwightDbContext();

                var save = false;
                await foreach (var settings in context.GuildSettings.AsAsyncEnumerable().WithCancellation(cancellationToken))
                    save |= await StartTimeReminderAsync(context, settings, cancellationToken);

                if (save)
                    await context.SaveChangesAsync(cancellationToken);
            }

            while (true)
            {
                try
                {
                    await ExecuteStartTimeRemindersAsync(cancellationToken);
                    await Task.Delay(_pollingConfiguration.StartTimePollingDuration, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    Logger.LogInformation("Shutting down start time service...");
                    break;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "An exception was thrown");
                }
            }
        }

        protected override async ValueTask OnReactionAdded(ReactionAddedEventArgs e)
        {
            if (!e.GuildId.HasValue || e.UserId == Bot.CurrentUser.Id)
                return;
            
            var guildId = e.GuildId.Value;
            if (!_startTimeMessageIdByGuildId.TryGetValue(guildId, out var messageId) || messageId != e.MessageId)
                return;
            
            var possibility = e.Emoji.Name switch
            {
                "✅" => "can",
                "❌" => "cannot",
                "⚠" => "can maybe",
                _ => null
            };

            if (possibility == null)
                return;

            var channel = (IMessageChannel) Bot.GetChannel(guildId, e.ChannelId);
            await channel.SendMessageAsync(new(){ Content = $"{Mention.User(e.UserId)} {possibility} start war" });
        }

        private async Task<bool> StartTimeReminderAsync(DwightDbContext context, GuildSettings settings, CancellationToken cancellationToken)
        {
            var guildId = settings.GuildId;
            Logger.LogInformation("Looking for start times for {GuildId}", guildId);

            if (Bot.GetChannel(guildId, settings.StartTimeChannelId) is not CachedTextChannel channel)
            {
                Logger.LogInformation("{GuildId} has not setup their start time channel", guildId);
                return false;
            }

            var calendarLink = settings.CalendarLink;
            if (string.IsNullOrWhiteSpace(calendarLink))
                return false;

            var calendarUri = GetUri(calendarLink);
            if (calendarUri == null)
            {
                settings.CalendarLink = null;
                Logger.LogError("{Guild} provided a malformed calendar link {CalendarLink}", guildId, calendarLink);

                return true;
            }

            var ical = await _httpClient.GetAsync(calendarUri, cancellationToken);
            var calendar = Calendar.Load(await ical.Content.ReadAsStreamAsync(cancellationToken));
            var nextEvent = calendar.Events.OrderByDescending(@event => @event.Start)
                .FirstOrDefault();

            if (nextEvent == null)
                return false;

            var nextEventUid = nextEvent.Uid;
            if (_lastEventIdByGuildId.TryGetValue(guildId, out var lastEvent) && nextEventUid == lastEvent)
            {
                Logger.LogInformation("No new event for {GuildId}", guildId);
                return false;
            }

            _lastEventIdByGuildId[guildId] = nextEventUid;

            var start = nextEvent.Start.AsDateTimeOffset.ToUniversalTime();
            var end = nextEvent.End.AsDateTimeOffset.ToUniversalTime();

            if (end < DateTimeOffset.UtcNow)
            {
                await channel.SendMessageAsync(new() { Content = "I found start times but they appear to be in the past..." });
                return false;
            }

            if (end - start != TWO_HOURS)
                await channel.SendMessageAsync(new() { Content = "The search window is less than 2hours, it's a sussy baka" });

            if (start - DateTimeOffset.UtcNow > TWENTY_FIVE_HOURS)
                await channel.SendMessageAsync(new() { Content = "The search window is oddly far into the future, something is afoot" });

            var reps = context.FwaReps.Where(rep => rep.DiscordId == guildId);
            var timesByRepId = await reps.ToDictionaryAsync(
                rep => rep.DiscordId, 
                rep => (GetTime(start, rep.TimeZone), GetTime(end, rep.TimeZone)),
                cancellationToken
            );

            var timesEmbed = CreateTimesEmbed(timesByRepId);
            await SendStartTimesMessageAsync(channel, timesEmbed, guildId);

            ScheduleStartTimeReminders(settings, start, channel);

            return false;
        }

        private void ScheduleStartTimeReminders(GuildSettings settings, DateTimeOffset start, CachedTextChannel channel)
        {
            var oneHourBeforeStart = start.AddHours(-1);
            var tenMinutesBeforeStart = start.AddMinutes(-10);

            // todo on cancel
            _espeonScheduler.DoAt(oneHourBeforeStart, channel, channel => channel.SendMessageAsync(new() { Content = $"{Mention.Everyone} search is in 1 hour!" }));
            _espeonScheduler.DoAt(tenMinutesBeforeStart, channel, channel => channel.SendMessageAsync(new() { Content = $"{Mention.Everyone} search is in 10 minutes!" }));
            _espeonScheduler.DoAt(start, (channel, _clashClient, settings.ClanTag), async state =>
            {
                var (channel, client, clanTag) = state;
                var builder = new StringBuilder($"{Markdown.Bold(Markdown.Underline("Members Ordered By Donations"))}");

                var clanMembers = await client.GetClanMembersAsync(clanTag);
                var clanMemberDonationsFormatted = clanMembers.OrderByDescending(x => x.Donations)
                    .Select((member, index) => $"{index + 1}: {Markdown.Escape(member.Name)} - {Markdown.Bold(member.Donations)}");

                builder.AppendJoin('\n', clanMemberDonationsFormatted);

                await channel.SendMessageAsync(new() { Content = builder.ToString() });
            });
        }

        private async Task SendStartTimesMessageAsync(CachedTextChannel channel, LocalEmbed timesEmbed, ulong guildId)
        {
            var message = await channel.SendMessageAsync(new() { Embeds = new List<LocalEmbed> { timesEmbed } });
            await message.AddReactionAsync(LocalEmoji.Unicode("✅"));
            await message.AddReactionAsync(LocalEmoji.Unicode("⚠"));
            await message.AddReactionAsync(LocalEmoji.Unicode("❌"));
            _startTimeMessageIdByGuildId[guildId] = message.Id;
        }

        private Uri GetUri(string calendarLink)
        {
            try
            {
                return new(calendarLink);
            }
            catch
            {
                return null;
            }
        }

        private DateTimeOffset GetTime(DateTimeOffset offset, float timeZone)
            => offset.Add(TimeSpan.FromHours(timeZone));

        private LocalEmbed CreateTimesEmbed(Dictionary<ulong, (DateTimeOffset, DateTimeOffset)> timesByRepId)
            => new LocalEmbed
                {
                    Title = "Sync Times Posted!",
                    Color = new Color(0x10c1f7),
                    Timestamp = DateTimeOffset.UtcNow
                }
                .AddField("Sync Times!", FormatTimes(timesByRepId))
                .AddField("\u200b", "React with ✅ if you can start war, ⚠ if maybe and, ❌ if not");

        private string FormatTimes(Dictionary<ulong, (DateTimeOffset, DateTimeOffset)> timesByRepId)
        {
            var formattedStartTimes = timesByRepId.Select(kvp => FormatStartTimeForRep(kvp.Key, kvp.Value));
            return string.Join('\n', formattedStartTimes);
        }

        private string FormatStartTimeForRep(ulong id, (DateTimeOffset Start, DateTimeOffset End) times)
            => $"{Bot.GetUser(id)?.Mention} : **Start**, {times.Start:dddd, dd MMMM yyyy HH:mm tt} - **End**, {times.End:dddd, dd MMMM yyyy HH:mm tt}";
    }
}