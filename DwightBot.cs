using System;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qmmands;

namespace Dwight;

public class DwightBot : DiscordBot
{
    public DwightBot(IOptions<DiscordBotConfiguration> options, ILogger<DwightBot> logger, IServiceProvider services, DiscordClient client, EspeonScheduler scheduler)
        : base(options, logger, services, client)
    {
        scheduler.OnError += OnSchedulerError;
    }

    private void OnSchedulerError(Exception ex)
    {
        Logger.LogError(ex, "An unhandled exception occurred in the scheduler");
    }

    protected override ValueTask<IResult> OnBeforeExecuted(IDiscordCommandContext context)
    {
        var command = context.Command!;
        var module = command.Module;
        var logger = (ILogger) Services.GetRequiredService(typeof(ILogger<>).MakeGenericType(module.TypeInfo!));

        logger.LogInformation("Executing {Module}#{Command}", module.Name, command.Name);

        return base.OnBeforeExecuted(context);
    }

    protected override ValueTask<bool> OnAfterExecuted(IDiscordCommandContext context, IResult result)
    {
        if (result is CommandNotFoundResult)
            return base.OnAfterExecuted(context, result);

        var command = context.Command!;
        var module = command.Module;
        var logger = (ILogger) Services.GetRequiredService(typeof(ILogger<>).MakeGenericType(module.TypeInfo!));

        logger.LogInformation("Executed {Module}#{Command} with result {Result}", module.Name, command.Name, result.FailureReason);

        return base.OnAfterExecuted(context, result);
    }
}