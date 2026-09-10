using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dwight;

public class FlareSolverrClient
{
    private readonly HttpClient _httpClient;
    private readonly FlareSolverrConfiguration _configuration;
    private readonly ILogger<FlareSolverrClient> _logger;

    public FlareSolverrClient(HttpClient httpClient, IOptions<FlareSolverrConfiguration> configuration, ILogger<FlareSolverrClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration.Value;
        _logger = logger;
    }

    public async Task<string?> GetAsync(string url, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Requesting {Url} via FlareSolverr", url);

        var request = new FlareSolverrRequest("request.get", url, _configuration.MaxTimeoutMilliseconds);

        using StringContent jsonContent = new(
            JsonSerializer.Serialize(request, FlareSolverrJsonOptions.Default),
            Encoding.UTF8,
            "application/json"
        );

        using var response = await _httpClient.PostAsync("/v1", jsonContent, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("FlareSolverr request for {Url} failed with {StatusCode}: {Content}", url, response.StatusCode, content);
            return null;
        }

        var solved = JsonSerializer.Deserialize<FlareSolverrResponse>(content, FlareSolverrJsonOptions.Default);
        if (solved is not { Status: "ok", Solution: not null })
        {
            _logger.LogError("FlareSolverr failed to resolve {Url}: {Message}", url, solved?.Message);
            return null;
        }

        return solved.Solution.Response;
    }
}
