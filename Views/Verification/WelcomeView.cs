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
                .WithContent($"{Mention.User(userId)} is only allowed to click this button")
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
                TimeSpan.FromMinutes(20)
            );

        await e.Interaction.Response().SendModalAsync(modalResponse);

        var modal = await waitForResponse;

        if (modal != null)
        {
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
                    .WithContent("Failed to verify, token expired (they only last a couple minutes) or wrong player tag specified")
                    .WithIsEphemeral();
                await modal.Response().SendMessageAsync(response);
                return;
            }

            if (!passwordResponse!.Value!.Equals(password, StringComparison.InvariantCultureIgnoreCase))
            {
                var response = new LocalInteractionMessageResponse()
                    .WithContent($"Incorrect RCS password, it can be found {Markdown.Link("here", "https://www.reddit.com/r/RedditClanSystem/wiki/official_reddit_clan_system/")}")
                    .WithIsEphemeral();
                await modal.Response().SendMessageAsync(response);
                return;
            }

            var completeResponse = new LocalInteractionMessageResponse()
                .WithContent("Thank you for doing the self verification, wait for a co-eld to accept you")
                .WithIsEphemeral();
            await modal.Response().SendMessageAsync(completeResponse);

            ClearComponents();

            var player = await apiClient.GetPlayerAsync(verifiedToken!.Tag, CancellationToken.None);
            var tag = verifiedToken.Tag;

            var tagString = Markdown.Escape($"{player?.Name}{tag}");
            var verifiedEmbed = new LocalEmbed()
                .WithTitle("Self Verification Complete")
                .WithColor(new(0x11f711))
                .WithDescription($"{modal.Author.Mention} has completed self verification, they own the account {Markdown.Code(tagString)}");

            await bot.SendMessageAsync(e.ChannelId, new() { Content = $"https://cc.fwafarm.com/cc_n/member.php?tag={tag[1..]}" });

            var verificationCompletedView = new VerificationCompletedView(
                message => message.WithEmbeds(verifiedEmbed),
                e.AuthorId,
                tag
            );
            var menu = new DefaultTextMenu(verificationCompletedView);
            await bot.StartMenuAsync(e.ChannelId, menu);
        }
        else
        {
            var response = new LocalInteractionMessageResponse()
                .WithContent("You did not respond...")
                .WithIsEphemeral();
            await e.Interaction.Response().SendMessageAsync(response);
        }
    }

    private static LocalEmbed CreateWelcomeEmbed(string guildName, Snowflake userId, Dictionary<string, string> baseLinkByLevel)
    {
        var description = new StringBuilder()
            .Append(Mention.User(userId))
            .Append(" welcome to ")
            .Append(guildName)
            .Append("!\nBelow are links to FWA approved bases for all Townhalls.\n");

        foreach (var keyValuePair in baseLinkByLevel)
        {
            var (level, url) = keyValuePair;
            description.Append(Markdown.Link($"TH{level}", url))
                .Append(", ");
        }

        var bigDescription = $"""
                              So that we know who you are please click the Verify Identity button, it will require your in game API key
                              * Settings > More Settings > API Token > Show > Copy
                              * And the {Markdown.Link("RCS Password", "https://www.reddit.com/r/RedditClanSystem/wiki/official_reddit_clan_system/")}
                              """;

        description.Append(bigDescription);

        return new()
        {
            Color = new(0x11f711),
            Title = $"Welcome to {guildName}!",
            Description = description.ToString()
        };
    }
}
