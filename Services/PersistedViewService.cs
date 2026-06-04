using System.Threading;
using System.Threading.Tasks;
using Disqord.Bot.Hosting;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Gateway;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dwight;

public class PersistedViewService : DiscordBotService
{
    private readonly TownhallConfiguration _townhallConfiguration;
    private int _reattached;

    public PersistedViewService(IOptions<TownhallConfiguration> townhallConfiguration)
    {
        _townhallConfiguration = townhallConfiguration.Value;
    }

    protected override async ValueTask OnReady(ReadyEventArgs e)
    {
        // READY fires again on every gateway resume; only reattach menus once per process.
        if (Interlocked.Exchange(ref _reattached, 1) == 1)
            return;

        await using var scope = Bot.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetDwightDbContext();

        var views = await context.PersistedViews.ToListAsync();

        foreach (var view in views)
        {
            try
            {
                await ReattachAsync(context, view);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, "Failed to reattach persisted view {MessageId} of type {Type}", view.MessageId, view.Type);
            }
        }

        Logger.LogInformation("Reattached {Count} persisted views", views.Count);
    }

    private async ValueTask ReattachAsync(DwightDbContext context, PersistedView view)
    {
        ViewBase? reconstructed = null;

        switch (view.Type)
        {
            case PersistedViewType.Welcome:
            {
                var settings = await context.GetOrCreateSettingsAsync(view.GuildId);
                if (settings.Password == null)
                {
                    Logger.LogWarning("Skipping welcome view {MessageId}, guild {GuildId} has no password set", view.MessageId, view.GuildId);
                    return;
                }

                var guild = Bot.GetGuild(view.GuildId);
                reconstructed = new WelcomeView(
                    guild?.Name ?? string.Empty,
                    _townhallConfiguration.BaseLinkByLevel,
                    view.UserId,
                    settings.Password
                );
                break;
            }
            case PersistedViewType.VerificationCompleted:
            {
                if (view.Tag == null)
                {
                    Logger.LogWarning("Skipping verification completed view {MessageId}, no tag stored", view.MessageId);
                    return;
                }

                reconstructed = new VerificationCompletedView(
                    _ => { },
                    view.UserId,
                    view.Tag
                );
                break;
            }
        }

        if (reconstructed == null)
            return;

        var menu = new DefaultTextMenu(reconstructed, view.MessageId);
        await Bot.StartMenuAsync(view.ChannelId, menu, Timeout.InfiniteTimeSpan);
    }
}
