using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Dwight;

public class FwaPointsClient
{
    private readonly FlareSolverrClient _flareSolverrClient;
    private readonly ILogger<FwaPointsClient> _logger;

    public FwaPointsClient(FlareSolverrClient flareSolverrClient, ILogger<FwaPointsClient> logger)
    {
        _flareSolverrClient = flareSolverrClient;
        _logger = logger;
    }

    // clanTag and opponentTag are expected without the leading '#'.
    public async Task<FwaWinPrediction?> GetWinPredictionAsync(string clanTag, string opponentTag, CancellationToken cancellationToken)
    {
        var url = $"https://points.fwafarm.com/clan?tag={clanTag}";
        var html = await _flareSolverrClient.GetAsync(url, cancellationToken);
        if (html == null)
            return null;

        var document = new HtmlDocument();
        document.LoadHtml(html);

        var winnerBox = document.DocumentNode.SelectSingleNode("//p[@class='winner-box']");
        if (winnerBox == null)
        {
            _logger.LogInformation("No win calculator found for {ClanTag}, it may not be FWA-tracked", clanTag);
            return null;
        }

        // The page's own war sync can lag behind the Clash API, so only trust the prediction if it is
        // actually talking about the opponent we're currently at war with.
        var linkedTags = winnerBox.SelectNodes(".//a[contains(@href, '/clan?tag=')]")?
            .Select(link => Regex.Match(link.GetAttributeValue("href", ""), "tag=([A-Z0-9]+)", RegexOptions.IgnoreCase))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value)
            .ToArray() ?? Array.Empty<string>();

        if (!linkedTags.Contains(opponentTag, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "FWA points page for {ClanTag} hasn't synced to the current opponent {OpponentTag} yet, skipping prediction",
                clanTag, opponentTag);
            return null;
        }

        var winnerName = HtmlEntity.DeEntitize(winnerBox.SelectSingleNode(".//b")?.InnerText)?.Trim();
        var lastLine = GetLines(winnerBox).LastOrDefault();
        if (string.IsNullOrEmpty(winnerName) || lastLine == null)
            return null;

        var reason = lastLine.StartsWith(winnerName, StringComparison.Ordinal)
            ? lastLine[winnerName.Length..].Trim()
            : lastLine;

        return new(winnerName, reason);
    }

    // HtmlAgilityPack flattens <br> separated text with no whitespace, so rebuild lines by hand.
    private static IReadOnlyList<string> GetLines(HtmlNode node)
    {
        var lines = new List<string>();
        var current = new StringBuilder();

        foreach (var child in node.ChildNodes)
        {
            if (child.Name == "br")
            {
                if (current.Length > 0)
                {
                    lines.Add(HtmlEntity.DeEntitize(current.ToString()).Trim());
                    current.Clear();
                }

                continue;
            }

            current.Append(child.InnerText);
        }

        if (current.Length > 0)
            lines.Add(HtmlEntity.DeEntitize(current.ToString()).Trim());

        return lines;
    }
}
