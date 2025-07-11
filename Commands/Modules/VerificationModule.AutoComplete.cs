﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord.Bot.Commands.Application;

namespace Dwight;

public partial class VerificationModule
{
    [AutoComplete("verify")]
    public async ValueTask VerifyAsync(AutoComplete<string> userTag)
    {
        var settings = await dbContext.GetOrCreateSettingsAsync(Context.GuildId, settings => settings.Members);
        if (userTag.IsFocused)
        {
            var inClan = settings.Members.SelectMany(member => member.Tags).ToHashSet();
            var members = await clashApiClient.GetClanMembersAsync(settings.ClanTag!, Context.CancellationToken);
            if (members == null)
                return;

            var notInClan = members.Where(member => !inClan.Contains(member.Tag))
                .Select(member => member.Tag)
                .Where(tag => userTag.RawArgument == null || tag.Contains(userTag.RawArgument, StringComparison.InvariantCultureIgnoreCase))
                .Take(25);
            userTag.Choices.AddRange(notInClan);
        }
    }
}
