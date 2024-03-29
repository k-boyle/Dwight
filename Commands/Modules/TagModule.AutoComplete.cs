﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord.Bot.Commands.Application;

namespace Dwight;

public partial class TagModule
{
    [AutoComplete("add")]
    public async ValueTask AddAltAsync(AutoComplete<string> tag)
    {
        var settings = await _dbContext.GetOrCreateSettingsAsync(Context.GuildId, settings => settings.Members);

        if (tag.IsFocused)
        {
            var inClan = settings.Members.SelectMany(member => member.Tags).ToHashSet();
            var members = await _clashApiClient.GetClanMembersAsync(settings.ClanTag!, Context.CancellationToken);
            if (members == null)
                return;

            var notInClan = members.Where(member => !inClan.Contains(member.Tag)).Select(member => member.Tag).Take(25);
            tag.Choices.AddRange(notInClan);
        }
    }

    [AutoComplete("remove")]
    public async ValueTask RemoveAltAsync(AutoComplete<string> member, AutoComplete<string> tag)
    {
        if (!member.Argument.HasValue && !tag.IsFocused)
            return;

        var memberId = ulong.Parse(member.Argument.Value);
        var clashMember = await _dbContext.Members.FindAsync(Context.GuildId.RawValue, memberId);
        if (clashMember == null)
            return;

        tag.Choices!.AddRange(clashMember.Tags);
    }

    [AutoComplete("set")]
    public async ValueTask SetMainAsync(AutoComplete<string> member, AutoComplete<string> tag)
    {
        if (!member.Argument.HasValue && !tag.IsFocused)
            return;

        var memberId = ulong.Parse(member.Argument.Value);
        var clashMember = await _dbContext.Members.FindAsync(Context.GuildId.RawValue, memberId);
        if (clashMember == null)
            return;

        tag.Choices!.AddRange(clashMember.Tags);
    }

    [AutoComplete("whomst")]
    public async ValueTask WhomstAsync(AutoComplete<string> name)
    {
        if (!name.IsFocused || name.RawArgument == null || name.RawArgument.Length == 0)
            return;

        var settings = await _dbContext.GetOrCreateSettingsAsync(Context.GuildId);

        var clanMembers = await _clashApiClient.GetClanMembersAsync(settings.ClanTag!, Context.CancellationToken);
        if (clanMembers == null)
            return;

        var potentialMembers = clanMembers.Select(member => member.Name)
            .Where(memberName => memberName.StartsWith(name.RawArgument, StringComparison.InvariantCultureIgnoreCase))
            .Take(25);
        
        name.Choices.AddRange(potentialMembers);
    }
}