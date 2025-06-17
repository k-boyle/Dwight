using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Rest;

namespace Dwight;

public class TheYearIs2025Service : DiscordBotService
{
    protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs e)
    {
        if (e.Message.Content.Length > 0 && e.Message.Content[0] != '!') return;

        await Bot.SendMessageAsync(e.ChannelId,
            new()
            {
                Content = "It's 2025, it's / commands",
                Reference = new LocalMessageReference { MessageId = e.MessageId }
            });
    }
}