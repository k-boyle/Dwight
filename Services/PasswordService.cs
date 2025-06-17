using System;
using System.Threading.Tasks;
using Disqord.Bot.Hosting;
using Disqord.Rest;
using Microsoft.Extensions.DependencyInjection;

namespace Dwight;

public class PasswordService : DiscordBotService
{
    protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs e)
    {
        var message = e.Message;
        if (message.Author.IsBot || message.GuildId == null) return;
        
        await using var scope = Bot.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetDwightDbContext();
        var settings = await dbContext.GetOrCreateSettingsAsync(message.GuildId.Value);

        if (message.ChannelId != settings.WelcomeChannelId) return;
        if (settings.Password == null || !message.Content.Contains(settings.Password, StringComparison.InvariantCultureIgnoreCase)) return;

        await message.DeleteAsync();

        await Bot.SendMessageAsync(message.ChannelId, new() { Content = $"{message.Author.Mention} said the correct password" });
    }
}