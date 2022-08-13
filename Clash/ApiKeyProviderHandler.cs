using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Dwight;

public class ApiKeyProviderHandler : DelegatingHandler
{
    private readonly ApiKeyProvider _apiKeyProvider;

    public ApiKeyProviderHandler(ApiKeyProvider apiKeyProvider)
    {
        _apiKeyProvider = apiKeyProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var apiKey = await _apiKeyProvider.GetKeyAsync(true, cancellationToken);
        request.Headers.Remove("Authorization");
        request.Headers.Add("Authorization", $"Bearer {apiKey}");

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Forbidden)
            return response;
        
        apiKey = await _apiKeyProvider.GetKeyAsync(false, cancellationToken);
        request.Headers.Remove("Authorization");
        request.Headers.Add("Authorization", $"Bearer {apiKey}");

        return await base.SendAsync(request, cancellationToken);
    }
}