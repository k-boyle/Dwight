using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Disqord.Bot.Hosting;
using Disqord.Rest;
using Microsoft.Extensions.DependencyInjection;

namespace Dwight;

public class ChocolateCakeService : DiscordBotService
{
    private static readonly Regex TAG_REGEX = new(@"\b#?\w{8,9}\b", RegexOptions.Compiled);

    private readonly ClashApiClient _client;

    public ChocolateCakeService(ClashApiClient client)
    {
        _client = client;
    }

    protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs e)
    {
        var message = e.Message;
        if (message.Author.IsBot || message.GuildId == null) return;

        var content = message.Content;
        var match = TAG_REGEX.Match(content);
        if (!match.Success) return;
        
        var tag = match.Value;
        if (tag[0] != '#')
            tag = $"#{tag}";
        
        var player = await _client.GetPlayerAsync(tag, CancellationToken.None);
        if (player == null) return;

        await using var scope = Bot.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetDwightDbContext();
        var settings = await dbContext.GetOrCreateSettingsAsync(message.GuildId.Value);
        
        if (message.ChannelId != settings.WelcomeChannelId) return;

        var reply = $"https://cc.fwafarm.com/cc_n/member.php?tag={tag[1..]}";

        await Bot.SendMessageAsync(message.ChannelId, new() { Content = reply });
    }
}