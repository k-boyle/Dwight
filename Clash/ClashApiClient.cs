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
        var clanMembers = await GetAsync(Endpoints.GetClanMembers.With(clanTag), cancellationToken);
        return clanMembers?.Items;
    }

    public Task<CurrentWar?> GetCurrentWarAsync(string clanTag, CancellationToken cancellationToken)
        => GetAsync(Endpoints.GetCurrentWar.With(clanTag), cancellationToken);

    public Task<LeagueGroup?> GetLeagueGroupAsync(string clanTag, CancellationToken cancellationToken)
        => GetAsync(Endpoints.GetLeagueGroup.With(clanTag), cancellationToken);

    public Task<CurrentWar?> GetLeagueWarAsync(string warTag, CancellationToken cancellationToken)
        => GetAsync(Endpoints.GetLeagueWar.With(warTag), cancellationToken);

    private async Task<T?> GetAsync<T>(Endpoint<T> endpoint, CancellationToken cancellationToken) where T : class
    {
        _logger.LogInformation("Executing {Endpoint} with {Parameter}", endpoint.PathTemplate, endpoint.Parameter);

        var fullPath = endpoint.Format();
        using var response = await _httpClient.GetAsync(fullPath, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
            return JsonConvert.DeserializeObject<T>(content);

        var failure = JsonConvert.DeserializeObject<ApiFailure>(content)!;
        _logger.LogError(failure, "Error executing {Endpoint} with {Parameter}", endpoint.PathTemplate, endpoint.Parameter);

        throw failure;
    }
}