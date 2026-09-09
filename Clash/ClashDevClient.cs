using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Dwight;

public class ClashDevClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClashDevClient> _logger;

    public ClashDevClient(HttpClient httpClient, ILogger<ClashDevClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> LoginAsync(LoginRequest login, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Logging into developer API");

        var body = JsonSerializer.Serialize(login, ClashJsonOptions.Default);
        var bodyContent = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await _httpClient.PostAsync("/api/login", bodyContent, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
            return response.Headers.GetValues("Set-Cookie").Single();

        var failure = JsonSerializer.Deserialize<DevApiFailure>(content, ClashJsonOptions.Default)!;
        _logger.LogError(failure, "Failed to authenticate with developer API");
        throw failure;
    }

    public async Task<ApiKeys> GetKeysAsync(string cookie, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting keys");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/apikey/list");
        request.Headers.Add("Cookie", cookie);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
            return JsonSerializer.Deserialize<ApiKeys>(content, ClashJsonOptions.Default)!;

        var failure = JsonSerializer.Deserialize<DevApiFailure>(content, ClashJsonOptions.Default)!;
        _logger.LogError(failure, "Failed to fetch api keys");
        throw failure;
    }

    public async Task RevokeKeyAsync(RevokeKey revokeKey, string cookie, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Revoking key with id {KeyId}", revokeKey.id);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/apikey/revoke")
        {
            Content = JsonContent.Create(revokeKey)
        };
        request.Headers.Add("Cookie", cookie);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
            return;

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var failure = JsonSerializer.Deserialize<DevApiFailure>(content, ClashJsonOptions.Default)!;
        _logger.LogError(failure, "Failed to revoke api key");
        throw failure;
    }

    public async Task<string> CreateKeyAsync(CreateKey createKey, string cookie, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching current ip address");

        using var ipResponse = await _httpClient.GetAsync("http://ipinfo.io/ip", cancellationToken);
        var ip = await ipResponse.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogInformation("Creating new api token");

        var body = new
        {
            name = createKey.name,
            description = createKey.description,
            cidrRanges = new string[] { ip },
            scopes = ""
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/apikey/create")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("Cookie", cookie);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
            return JsonSerializer.Deserialize<CreateKeyResponse>(content, ClashJsonOptions.Default)!.Key.Key;

        var failure = JsonSerializer.Deserialize<DevApiFailure>(content, ClashJsonOptions.Default)!;
        _logger.LogError(failure, "Failed to create new api key");
        throw failure;
    }

    private record CreateKeyResponse(CreateKeyResponseKey Key);

    private record CreateKeyResponseKey(string Key);
}