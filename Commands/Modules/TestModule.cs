using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Dwight;

public class TestModule : DiscordApplicationModuleBase
{
    [SlashCommand("ping")]
    [Description("🏓")]
    public IResult Ping()
        => Response("pong");
}