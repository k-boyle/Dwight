using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Extensions.Interactivity;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Rest;
using Microsoft.Extensions.DependencyInjection;

namespace Dwight;

public class WelcomeView(string guildName, Dictionary<string, string> baseLinkByLevel, Snowflake userId, string password)
    : ViewBase(message => message.WithContent(Mention.User(userId)).WithEmbeds(CreateWelcomeEmbed(guildName, userId, baseLinkByLevel)))
{
    [Button(Emoji = "🕵️", Label = "Verify Identity")]
    public async ValueTask VerifyButtonAsync(ButtonEventArgs e)
    {
        if (e.AuthorId != userId)
        {
            var notYou = new LocalInteractionMessageResponse()
                .WithContent($"This button belongs to {Mention.User(userId)}. You are not {Mention.User(userId)}. Hands off.")
                .WithIsEphemeral();
            await e.Interaction.Response().SendMessageAsync(notYou);
            return;
        }

        const string modalId = "self-verify-modal";
        const string playerTagInputId = "player-tag-input";
        const string playerApiKeyInputId = "player-api-key-input";
        const string passwordInputId = "password-input";

        var tagInput = new LocalTextInputComponent()
            .WithCustomId(playerTagInputId)
            .WithStyle(TextInputComponentStyle.Short)
            .WithLabel("In Game Player Tag (e.g. #YRQ2Y0UC)");
        var keyInput = new LocalTextInputComponent()
            .WithCustomId(playerApiKeyInputId)
            .WithStyle(TextInputComponentStyle.Short)
            .WithLabel("In Game Player ApiKey (e.g. k8sxbxpa)");
        var passwordInput = new LocalTextInputComponent()
            .WithCustomId(passwordInputId)
            .WithStyle(TextInputComponentStyle.Short)
            .WithLabel("RCS Password");

        var row1 = LocalComponent.Row(tagInput);
        var row2 = LocalComponent.Row(keyInput);
        var row3 = LocalComponent.Row(passwordInput);

        var modalResponse = new LocalInteractionModalResponse()
            .WithTitle("Self Verification")
            .WithComponents(row1, row2, row3)
            .WithCustomId(modalId);

        var waitForResponse = Menu.Client.GetInteractivity()
            .WaitForInteractionAsync<IModalSubmitInteraction>(
                e.ChannelId, modal => modal.CustomId == modalId && modal.AuthorId == e.AuthorId,
                Timeout.InfiniteTimeSpan
            );

        await e.Interaction.Response().SendModalAsync(modalResponse);

        var modal = await waitForResponse;

        if (modal == null)
            return;

        var components = modal.Components;
        var row1Response = components[0] as IRowComponent;
        var row2Response = components[1] as IRowComponent;
        var row3Response = components[2] as IRowComponent;

        var tagResponse = row1Response!.Components[0] as ITextInputComponent;
        var keyResponse = row2Response!.Components[0] as ITextInputComponent;
        var passwordResponse = row3Response!.Components[0] as ITextInputComponent;

        var bot = e.Interaction.Client as DiscordBot;
        var apiClient = bot!.Services.GetRequiredService<ClashApiClient>();

        var verifiedToken = await apiClient.VerifyTokenAsync(tagResponse!.Value!, new VerifyToken(keyResponse!.Value!), CancellationToken.None);
        if (verifiedToken?.Status != "ok")
        {
            var response = new LocalInteractionMessageResponse()
                .WithContent("Verification denied. Either your token expired — they last only a couple of minutes, much like trust — or you handed me the wrong player tag. Try again, and this time, concentrate.")
                .WithIsEphemeral();
            await modal.Response().SendMessageAsync(response);
            return;
        }

        if (!passwordResponse!.Value!.Equals(password, StringComparison.InvariantCultureIgnoreCase))
        {
            var response = new LocalInteractionMessageResponse()
                .WithContent($"Wrong password. Security is my passion, and you have failed it. The correct one is documented {Markdown.Link("here", "https://www.reddit.com/r/RedditClanSystem/wiki/official_reddit_clan_system/")}. Read it.")
                .WithIsEphemeral();
            await modal.Response().SendMessageAsync(response);
            return;
        }

        var completeResponse = new LocalInteractionMessageResponse()
            .WithContent("Self verification complete. Your paperwork is in order. Now you wait. A co-elder will review your case and decide your fate. Do not rush a co-elder.")
            .WithIsEphemeral();
        await modal.Response().SendMessageAsync(completeResponse);

        ClearComponents();

        await using var scope = bot.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetDwightDbContext();

        // The welcome view is done with; drop it so it is not reattached on restart.
        await context.RemoveViewAsync(Menu.MessageId);

        var player = await apiClient.GetPlayerAsync(verifiedToken!.Tag, CancellationToken.None);
        var tag = verifiedToken.Tag;

        var tagString = Markdown.Escape($"{player?.Name}{tag}");
        var verifiedEmbed = new LocalEmbed()
            .WithTitle("Self Verification Complete")
            .WithColor(new(0x11f711))
            .WithDescription($"{modal.Author.Mention} has completed self verification. I have confirmed, personally, that they are the rightful owner of the account {Markdown.Code(tagString)}. The records do not lie.");

        await bot.SendMessageAsync(e.ChannelId, new() { Content = $"https://cc.fwafarm.com/cc_n/member.php?tag={tag[1..]}" });

        var verificationCompletedView = new VerificationCompletedView(
            message => message.WithEmbeds(verifiedEmbed),
            e.AuthorId,
            tag
        );
        var menu = new DefaultTextMenu(verificationCompletedView);
        await bot.StartMenuAsync(e.ChannelId, menu, Timeout.InfiniteTimeSpan);

        await context.UpsertViewAsync(new PersistedView(
            menu.MessageId,
            e.ChannelId,
            e.GuildId!.Value,
            PersistedViewType.VerificationCompleted,
            e.AuthorId,
            tag
        ));
    }

    private static LocalEmbed CreateWelcomeEmbed(string guildName, Snowflake userId, Dictionary<string, string> baseLinkByLevel)
    {
        var description = new StringBuilder()
            .Append(Mention.User(userId))
            .Append(", welcome to ")
            .Append(guildName)
            .Append(". I am the new Sheriff. Below are the FWA approved bases for every Townhall. These are not suggestions. Use them.\n");

        foreach (var keyValuePair in baseLinkByLevel)
        {
            var (level, url) = keyValuePair;
            description.Append(Markdown.Link($"TH{level}", url))
                .Append(", ");
        }

        var bigDescription = $"""
                              Before you set one foot inside, I must establish who you are. Identity theft is rampant. Click the Verify Identity button. It will require your in game API key.
                              * Settings > More Settings > API Token > Show > Copy
                              * And the {Markdown.Link("RCS Password", "https://www.reddit.com/r/RedditClanSystem/wiki/official_reddit_clan_system/")}
                              """;

        description.Append(bigDescription);

        return new()
        {
            Color = new(0x11f711),
            Title = $"Welcome to {guildName}. State Your Business.",
            Description = description.ToString()
        };
    }
}
