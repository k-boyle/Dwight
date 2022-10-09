using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using HtmlAgilityPack;
using Qmmands;

namespace Dwight;

[SlashGroup("cwl")]
[RequireClanTag]
public class CwlCommands : DiscordApplicationGuildModuleBase
{
    private readonly SheetsService _sheetsService;
    private readonly ClashApiClient _clashApiClient;
    private readonly HttpClient _httpClient;
    private readonly DwightDbContext _dwightDbContext;

    public CwlCommands(SheetsService sheetsService, ClashApiClient clashApiClient, HttpClient httpClient, DwightDbContext dwightDbContext)
    {
        _sheetsService = sheetsService;
        _clashApiClient = clashApiClient;
        _dwightDbContext = dwightDbContext;
        _httpClient = httpClient;
    }

    [SlashCommand("update")]
    [RequireAuthorPermissions(Permissions.Administrator)]
    [Description("Update the cwl spreadsheet")]
    [RateLimit(1, 5, RateLimitMeasure.Minutes, RateLimitBucketType.Guild)]
    public async ValueTask<IResult> UpdateSpreadsheetAsync()
    {
        var settings = await _dwightDbContext.GetOrCreateSettingsAsync(Context.GuildId);

        if (settings.CwlSheetId == null)
            return Response("You need to set your sheet id");

        var clanTag = settings.ClanTag!;
        
        var group = await _clashApiClient.GetLeagueGroupAsync(clanTag, Context.CancellationToken);
        if (group == null)
            return Response("Currently not in CWL");

        await Deferral(true);

        var weightPage = await _httpClient.GetAsync($"http://fwastats.com/Clan/{clanTag.Replace("#", "")}/Weight", Context.CancellationToken);
        var content = await weightPage.Content.ReadAsStringAsync(Context.CancellationToken);

        var document = new HtmlDocument();
        document.LoadHtml(content);
        
        var memberIndexes = document.DocumentNode.SelectSingleNode("/html/body/div[2]/form[2]/table/tbody").SelectNodes("tr")
            .Select((node, index) => (node.SelectSingleNode("td[2]/input").GetAttributeValue("value", ""), index))
            .ToDictionary(pair => pair.Item1, pair => pair.index);
        
        var rows = Enumerable.Range(0, 51).Select(_ =>
        {
            // lol
            var playerRow = new List<object>(new object[9]);
            return (IList<object>) playerRow;
        }).ToList();
        var titleRow = rows[0];
        titleRow[0] = "Name";
        titleRow[1] = "Current Opted State";
        
        foreach (var (tag, index) in memberIndexes)
        {
            var player = await _clashApiClient.GetPlayerAsync(tag, Context.CancellationToken);
            if (player == null)
                return Response($"{tag} was in the weights but not in the clan");

            var playerRow = rows[1 + index];
            playerRow[0] = player.Name;
            playerRow[1] = player.WarPreference.ToString();
        }

        for (int r = 0, warIndex = 0; r < group.Rounds.Length; r++, warIndex++)
        {
            var round = group.Rounds[r];
            foreach (var tag in round.WarTags)
            {
                var war = await _clashApiClient.GetLeagueWarAsync(tag, Context.CancellationToken);
                var clans = war!.Clan.Tag == clanTag
                    ? (war.Clan, war.Opponent)
                    : war.Opponent.Tag == clanTag
                        ? (war.Opponent, war.Clan)
                        : ((WarClan Opponent, WarClan Clan)?)null;

                if (clans == null)
                    continue;

                var (clan, opponent) = clans.Value;
                titleRow[2 + warIndex] = opponent.Name;

                if (war.State is WarState.Preparation or WarState.NotInWar)
                {
                    for (int i = 1; i < 51; i++)
                    {
                        var playerRow = rows[i];
                        if (playerRow.Count > 0)
                        {
                            playerRow[2 + warIndex] = "";
                        }
                    }

                    continue;
                }

                foreach (var member in clan.Members)
                {
                    if (memberIndexes.TryGetValue(member.Tag, out var index))
                    {
                        var playerRow = rows[1 + index];
                        playerRow[2 + warIndex] = member.Attacks == null ? "Impish" : "Admirable";
                    }
                    else
                    {
                        throw new("????");
                    }
                }
            }
        }
        
        foreach (var row in rows)
        {
            var empty = row[0] == null!;
            for (var i = 0; i < row.Count; i++)
            {
                var item = (object?) row[i];
                row[i] = item ?? (empty ? "" : "Out");
            }
        }

        var valueRange = new ValueRange
        {
            Values = rows
        };

        var values = _sheetsService.Spreadsheets.Values;

        var sheetsRequest = values.Update(valueRange, settings.CwlSheetId, "'the office'!A1:Z1000");
        sheetsRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

        await sheetsRequest.ExecuteAsync();

        return Response("Updated CWL sheet");
    }
}