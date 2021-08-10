using System;
using Disqord;
using Disqord.Bot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dwight
{
    public class DwightBot : DiscordBot
    {
        public DwightBot(IOptions<DiscordBotConfiguration> options, ILogger<DwightBot> logger, IServiceProvider services, DiscordClient client)
            : base(options, logger, services, client)
        {
        }
    }
}