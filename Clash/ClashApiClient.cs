using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Dwight;

public class ClashApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClashApiClient> _logger;

    public ClashApiClient(HttpClient httpClient, ILogger<ClashApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<ClanMember>?> GetClanMembersAsync(string clanTag, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting clan members for {ClanTag}", clanTag);

        var endpoint = $"/v1/clans/{Uri.EscapeDataString(clanTag)}/members";
        using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        
        if (!response.IsSuccessStatusCode)
        {
            var failure = JsonConvert.DeserializeObject<ApiFailure>(content)!;
            _logger.LogError(failure, "Error whilst fetching clan member for {ClanTag}", clanTag);
            throw failure;
        }

        var model = new
        {
            items = Array.Empty<ClanMember>()
        };

        return JsonConvert.DeserializeAnonymousType(content, model)!.items;
    }

    public async Task<CurrentWar?> GetCurrentWarAsync(string clanTag, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting current war for {ClanTag}", clanTag);

        var endpoint = $"/v1/clans/{Uri.EscapeDataString(clanTag)}/currentwar";
        using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (response.IsSuccessStatusCode)
            return JsonConvert.DeserializeObject<CurrentWar>(content);

        var failure = JsonConvert.DeserializeObject<ApiFailure>(content)!;
        _logger.LogError(failure, "Error whilst fetching current war for {ClanTag}", clanTag);
        throw failure;

    }
}