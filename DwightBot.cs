using System;
using Disqord;
using Disqord.Bot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qmmands;

namespace Dwight
{
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

        protected override LocalMessage FormatFailureMessage(DiscordCommandContext context, FailedResult result)
        {
            var message = base.FormatFailureMessage(context, result);
            message.Embeds[0].ImageUrl = "https://cdn.discordapp.com/attachments/376093944385241102/875461673413144586/200.png";
            return message;
        }
    }
}