using System;
using System.Threading;
using System.Threading.Tasks;
using ClashWrapper;
using ClashWrapper.Entities.War;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dwight.Services;

public class WarReminderService : DiscordBotService
{
    private static readonly TimeSpan ACCEPTABLE_WAR_THRESHOLD = TimeSpan.FromHours(1);
        
    private readonly PollingConfiguration _pollingConfiguration;
    private readonly EspeonScheduler _scheduler;
    private readonly ClashClient _clashClient;

    private ScheduledTask<CancellationToken> _loopTask;

    public WarReminderService(IOptions<PollingConfiguration> pollingConfiguration, EspeonScheduler scheduler, ClashClient clashClient)
    {
        _scheduler = scheduler;
        _clashClient = clashClient;
        _pollingConfiguration = pollingConfiguration.Value;

        _clashClient.Error += message =>
        {
            if (message.Reason != "inMaintenance")
                return Task.CompletedTask;

            _loopTask!.Cancel();
            // _loopTask = _scheduler.DoIn(_pollingConfiguration.WarReminderPollingDuration, _loopTask.State, CheckForWarsAsync);

            return Task.CompletedTask;
        };
    }
        
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_pollingConfiguration.WarReminderEnabled)
            return;

        await Bot.WaitUntilReadyAsync(stoppingToken);

        _loopTask = _scheduler.DoNow(stoppingToken, CheckForWarsAsync);
    }

    // todo handle maintenance
    // todo logs
    private async Task CheckForWarsAsync(CancellationToken cancellationToken)
    {
        await using var scope = Bot.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetDwightDbContext();

        await foreach (var settings in context.GuildSettings.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            if (Bot.GetChannel(settings.GuildId, settings.WarChannelId) is not IMessageChannel channel)
                continue;
                
            if (string.IsNullOrEmpty(settings.ClanTag))
                continue;

            if (settings.WarRole == 0)
                continue;

            var currentWar = await _clashClient.GetCurrentWarAsync(settings.ClanTag);
                
            if (currentWar == null || currentWar.State is WarState.Default or WarState.Ended)
                continue;

            switch (currentWar.State)
            {
                // case WarState.Preparation:
                // {
                //     var sinceMatched = DateTimeOffset.UtcNow - currentWar.PreparationTime;
                //     if (sinceMatched < ACCEPTABLE_WAR_THRESHOLD)
                //         continue;
                //
                //     var startTime = currentWar.StartTime - DateTimeOffset.UtcNow;
                //     _scheduler.DoIn(startTime, cancellationToken, WarReminders); // todo
                //     break;
                // }
                //
                // case WarState.InWar:
                // {
                //     var sinceMatched = DateTimeOffset.UtcNow - currentWar.PreparationTime;
                //     if (sinceMatched < ACCEPTABLE_WAR_THRESHOLD)
                //         continue;
                //
                //     _scheduler.DoNow(cancellationToken, WarReminders); // todo
                //     break;
                // }
            }
        }

        _loopTask = _scheduler.DoIn(_pollingConfiguration.WarReminderPollingDuration, cancellationToken, CheckForWarsAsync);
    }
    //
    // private Task WarReminders(CancellationToken cancellationToken, ulong warRole, IMessageChannel warChannel)
    // {
    //     
    // }
}