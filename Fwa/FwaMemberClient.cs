using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Dwight;

public class FwaMemberClient
{
    private readonly FlareSolverrClient _flareSolverrClient;
    private readonly ILogger<FwaMemberClient> _logger;

    public FwaMemberClient(FlareSolverrClient flareSolverrClient, ILogger<FwaMemberClient> logger)
    {
        _flareSolverrClient = flareSolverrClient;
        _logger = logger;
    }

    // playerTag is expected without the leading '#'.
    public async Task<FwaMemberStatus?> GetStatusAsync(string playerTag, CancellationToken cancellationToken)
    {
        var url = $"https://cc.fwafarm.com/cc_n/member.php?tag={playerTag}";
        var html = await _flareSolverrClient.GetAsync(url, cancellationToken);
        if (html == null)
            return null;

        var document = new HtmlDocument();
        document.LoadHtml(html);

        var top = document.DocumentNode.SelectSingleNode("//span[@id='top']");
        if (top == null)
        {
            _logger.LogInformation("No ChocolateClash details found for {PlayerTag}", playerTag);
            return null;
        }

        var topText = HtmlEntity.DeEntitize(top.InnerText);
        var isBanned = topText.Contains("BANNED!", StringComparison.Ordinal);

        var currentClanSegment = Regex.Match(topText, "Current Clan:(?<segment>.*?)Donates:", RegexOptions.Singleline);
        var currentClan = currentClanSegment.Success
            ? GetBlacklistedClan(currentClanSegment.Groups["segment"].Value, top.SelectSingleNode(".//a[starts-with(@href, 'clan.php?tag=')]"))
            : null;

        var trackedRows = document.DocumentNode.SelectNodes("//table//tr[td/a[starts-with(@href, 'clan.php?tag=')]]")
            ?? Enumerable.Empty<HtmlNode>();

        // Unlike the current-clan line above, the tracked-actions table links wrap the clan name rather
        // than the tag, so the name can be read straight off the link instead of the surrounding text.
        var history = trackedRows.Select(row =>
        {
            var link = row.SelectSingleNode(".//td/a[starts-with(@href, 'clan.php?tag=')]")!;
            var cellText = HtmlEntity.DeEntitize(link.ParentNode!.InnerText);
            return GetBlacklistedClan(cellText, link, useNameFromLink: true);
        });

        var flagged = currentClan ?? history.FirstOrDefault(association => association != null);

        return new(isBanned, flagged);
    }

    private static FwaClanAssociation? GetBlacklistedClan(string text, HtmlNode? clanLink, bool useNameFromLink = false)
    {
        if (clanLink == null || !text.Contains("Blacklist", StringComparison.OrdinalIgnoreCase))
            return null;

        var tagMatch = Regex.Match(clanLink.GetAttributeValue("href", ""), "tag=([A-Z0-9]+)", RegexOptions.IgnoreCase);
        if (!tagMatch.Success)
            return null;

        string clanName;
        if (useNameFromLink)
        {
            clanName = HtmlEntity.DeEntitize(clanLink.InnerText).Trim();
        }
        else
        {
            var nameMatch = Regex.Match(text, @"^\s*(?<name>[^(]*)\(");
            clanName = nameMatch.Success ? nameMatch.Groups["name"].Value.Trim() : tagMatch.Groups[1].Value;
        }

        return new(clanName, tagMatch.Groups[1].Value);
    }
}
