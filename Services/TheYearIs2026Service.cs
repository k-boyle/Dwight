using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Rest;

namespace Dwight;

public class TheYearIs2026Service : DiscordBotService
{
    protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs e)
    {
        if (e.Message.Author.IsBot || e.Message.Content.Length == 0 || e.Message.Content[0] != '!') return;

        await Bot.SendMessageAsync(e.ChannelId,
            new()
            {
                Content = "It is 2026. We use slash commands now. Adapt or be left behind.",
                Reference = new LocalMessageReference { MessageId = e.MessageId }
            });
    }
}
