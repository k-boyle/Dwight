using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qmmands;
using Qommon.Metadata;

namespace Dwight;

public class DwightBot : DiscordBot
{
    public DwightBot(IOptions<DiscordBotConfiguration> options, ILogger<DwightBot> logger, IServiceProvider services, DiscordClient client)
        : base(options, logger, services, client)
    {
    }
    
    protected override ValueTask<IResult> OnBeforeExecuted(IDiscordCommandContext context)
    {
        var command = context.Command!;
        var module = command.Module;
        var logger = (ILogger) Services.GetRequiredService(typeof(ILogger<>).MakeGenericType(module.TypeInfo!));

        logger.LogInformation("Executing {Module}#{Command}", module.Name, command.Name);
        context.SetMetadata("stopwatch", Stopwatch.StartNew());

        return base.OnBeforeExecuted(context);
    }

    protected override ValueTask<bool> OnAfterExecuted(IDiscordCommandContext context, IResult result)
    {
        var stopwatch = context.GetMetadata<Stopwatch>("stopwatch");
        stopwatch?.Stop();
        
        if (result is CommandNotFoundResult)
            return base.OnAfterExecuted(context, result);

        var command = context.Command!;
        var module = command.Module;
        var logger = (ILogger) Services.GetRequiredService(typeof(ILogger<>).MakeGenericType(module.TypeInfo!));

        logger.LogInformation(
            "Executed {Module}#{Command} with failure {Result} in {Time}",
            module.Name,
            command.Name, 
            result.FailureReason,
            stopwatch?.Elapsed
        );

        return base.OnAfterExecuted(context, result);
    }

    protected override ValueTask OnCommandResult(IDiscordCommandContext context, IDiscordCommandResult result)
    {
        try
        {
            return base.OnCommandResult(context, result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception thrown whilst executing OnCommandResult");
            return ValueTask.CompletedTask;
        }
    }
}