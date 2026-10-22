using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dwight;

public class PersistedViewService : DiscordBotService
{
    private int _reattached;

    protected override async ValueTask OnReady(ReadyEventArgs e)
    {
        // READY fires again on every gateway resume; only reattach menus once per process.
        if (Interlocked.Exchange(ref _reattached, 1) == 1)
            return;

        await using var scope = Bot.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetDwightDbContext();

        // Message IDs are snowflakes, so they double as the view's creation time. Anything older than a month
        // has been abandoned; stop reattaching it so the table does not grow forever.
        ulong cutoff = new Snowflake(DateTimeOffset.UtcNow.AddMonths(-1));
        var expired = await context.PersistedViews
            .Where(view => view.MessageId < cutoff)
            .ExecuteDeleteAsync();

        if (expired > 0)
            Logger.LogInformation("Dropped {Count} persisted views older than a month", expired);

        var views = await context.PersistedViews.ToListAsync();

        foreach (var view in views)
        {
            try
            {
                await ReattachAsync(context, view);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to reattach persisted view {MessageId} of type {Type}", view.MessageId, view.Type);
            }
        }

        Logger.LogInformation("Reattached {Count} persisted views", views.Count);
    }

    private async ValueTask ReattachAsync(DwightDbContext context, PersistedView view)
    {
        PersistentViewBase? reconstructed = null;

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
                var baseLinkByLevel = await context.GetTownhallBaseLinksAsync(view.GuildId);
                reconstructed = new WelcomeView(
                    guild?.Name ?? string.Empty,
                    baseLinkByLevel,
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

        if (await Bot.FetchMessageAsync(view.ChannelId, view.MessageId) is not IUserMessage message)
        {
            Logger.LogInformation("Dropping persisted view {MessageId}, its message was deleted", view.MessageId);
            await context.RemoveViewAsync(view.MessageId);
            return;
        }

        var menu = new DefaultTextMenu(reconstructed, view.MessageId);
        await Bot.StartMenuAsync(view.ChannelId, menu, Timeout.InfiniteTimeSpan);

        // Messages sent before views had stable custom IDs still carry random ones the rebuilt view can never
        // match, and a renamed button would do the same, so clicks go unanswered. Push the rebuilt components
        // onto those. Only the components are touched; the content and embeds were rendered with data that may
        // not be cached yet.
        var sentIds = message.Components
            .OfType<IRowComponent>()
            .SelectMany(row => row.Components)
            .OfType<ICustomIdentifiableEntity>()
            .Select(component => component.CustomId);
        var rebuiltIds = reconstructed.EnumerateComponents()
            .OfType<InteractableViewComponent>()
            .Select(component => component.CustomId);

        if (sentIds.SequenceEqual(rebuiltIds))
            return;

        var rebuilt = menu.CreateLocalMessage();
        reconstructed.FormatLocalMessage(rebuilt);
        await Bot.ModifyMessageAsync(view.ChannelId, view.MessageId, x => x.Components = new(rebuilt.Components.Value));
        Logger.LogInformation("Resynced stale buttons on persisted view {MessageId}", view.MessageId);
    }
}
