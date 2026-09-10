using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Rest;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dwight;

public class ChocolateCakeService : DiscordBotService
{
    private static readonly Regex TAG_REGEX = new(@"\b#?\w{8,9}\b", RegexOptions.Compiled);

    private readonly ClashApiClient _client;
    private readonly FwaMemberClient _fwaMemberClient;

    public ChocolateCakeService(ClashApiClient client, FwaMemberClient fwaMemberClient)
    {
        _client = client;
        _fwaMemberClient = fwaMemberClient;
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

        var bareTag = tag[1..];
        var reply = $"https://cc.fwafarm.com/cc_n/member.php?tag={bareTag}";

        var status = await GetFwaStatusAsync(bareTag, CancellationToken.None);
        if (status is { IsBanned: true })
        {
            reply += $"\n\n🚨 {Markdown.Bold("THIS ACCOUNT IS FWA BANNED.")} Do not verify them.";
        }
        else if (status?.BlacklistedClan is { } blacklistedClan)
        {
            reply += $"\n\nHeads up: their ChocolateClash history shows time in {Markdown.Bold(blacklistedClan.ClanName)} " +
                $"(https://cc.fwafarm.com/cc_n/clan.php?tag={blacklistedClan.ClanTag}), which is FWA blacklisted. Might be worth keeping an eye on them.";
        }

        await Bot.SendMessageAsync(message.ChannelId, new() { Content = reply });
    }

    private async Task<FwaMemberStatus?> GetFwaStatusAsync(string playerTag, CancellationToken cancellationToken)
    {
        try
        {
            return await _fwaMemberClient.GetStatusAsync(playerTag, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to fetch FWA member status for {PlayerTag}", playerTag);
            return null;
        }
    }
}