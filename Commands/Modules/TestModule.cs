using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Dwight;

public class TestModule : DiscordApplicationModuleBase
{
    [SlashCommand("ping")]
    [Description("🏓")]
    public IResult Ping()
        => Response("pong");

    [SlashCommand("sauce")]
    [Description("The good sauce")]
    public IResult Sauce()
        => Response("https://github.com/k-boyle/Dwight/");
}
