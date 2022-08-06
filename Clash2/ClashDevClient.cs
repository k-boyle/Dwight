using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Dwight;

public class ClashDevClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClashDevClient> _logger;

    public ClashDevClient(IHttpClientFactory httpClientFactory, ILogger<ClashDevClient> logger)
    {
        _httpClient = httpClientFactory.CreateClient(nameof(ClashDevClient));
        _httpClient.BaseAddress = new("https://developer.clashofclans.com/api/");

        _logger = logger;
    }

    public async Task<Login?> LoginAsync(LoginRequest login, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Logging into developer API");

        var body = JsonConvert.SerializeObject(login);
        var bodyContent = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await _httpClient.PostAsync("login", bodyContent, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode) 
            return JsonConvert.DeserializeObject<Login>(content);
        
        var failure = JsonConvert.DeserializeObject<LoginFailure>(content);
        _logger.LogError("Failed to authenticate with developer API due to {LoginFailure}", failure);
        return null;
    }
}