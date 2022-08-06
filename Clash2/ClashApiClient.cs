﻿using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Dwight;

public class ClashApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClashApiClient> _logger;
    private readonly TokenProvider _tokenProvider;

    public ClashApiClient(IHttpClientFactory httpClientFactory, ILogger<ClashApiClient> logger, TokenProvider tokenProvider)
    {
        _httpClient = httpClientFactory.CreateClient(nameof(ClashApiClient));
        _logger = logger;
        _tokenProvider = tokenProvider;
    }

    public async Task<ApiResponse<IReadOnlyCollection<ClanMember>>> GetClanMembersAsync(string clanTag, CancellationToken cancellationToken)
    {
        var token = await _tokenProvider.GetTokenAsync(cancellationToken);
        
    }
}
