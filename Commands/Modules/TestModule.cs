using Disqord.Bot;
using Qmmands;

namespace Dwight
{
    public class TestModule : DiscordGuildModuleBase
    {
        [Command("ping")]
        public CommandResult Ping()
            => Reply("pong");
    }
}