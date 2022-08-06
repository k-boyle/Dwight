using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClashWrapper.Entities;
using ClashWrapper.Models;
using ClashWrapper.RequestParameters;
using Newtonsoft.Json;

namespace ClashWrapper;

internal class RequestClient
{
    private readonly ClashClient _client;
    private readonly ClashClientConfig _config;
    private readonly HttpClient _httpClient;
    private readonly CookieContainer _cookieContainer;
    private readonly SemaphoreSlim _tokenSemaphore;

    private string _currentToken;

    private const string BaseUrl = "https://api.clashofclans.com";

    public RequestClient(ClashClient client, ClashClientConfig config)
    {
        _client = client;
        _config = config;

        _cookieContainer = new();
        _httpClient = new(new HttpClientHandler { CookieContainer = _cookieContainer })
        {
            BaseAddress = new(BaseUrl)
        };

        _httpClient.DefaultRequestHeaders.Accept.Add(new("application/json"));

        _tokenSemaphore = new(1);
    }

    public async Task<T> SendAsync<T>(string endpoint, BaseParameters parameters = null)
    {
        if (endpoint[0] != '/')
            throw new ArgumentException($"{nameof(endpoint)} must start with a '/'");

        parameters = parameters ?? new EmptyParameters();

        if (_currentToken == null)
        {
            await _tokenSemaphore.WaitAsync();

            if (_currentToken == null)
            {
                var sessionCookie = await LoginAsync();
                await DeleteOldKeyAsync(sessionCookie);
                _currentToken = await CreateNewKeyAsync(sessionCookie);
            }

            _tokenSemaphore.Release();
        }

        var request = new HttpRequestMessage(HttpMethod.Get, endpoint)
        {
            Content = new StringContent(parameters.BuildContent())
        };
        request.Headers.Add("Authorization", $"Bearer {_currentToken}");

        try
        {
            using (var response = await _httpClient.SendAsync(request).ConfigureAwait(false))
            {
                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    // :^}
                    await _tokenSemaphore.WaitAsync();
                    _currentToken = null;
                    _tokenSemaphore.Release();
                    return await SendAsync<T>(endpoint, parameters);
                }
                
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (response.IsSuccessStatusCode) return JsonConvert.DeserializeObject<T>(content);

                var model = JsonConvert.DeserializeObject<ErrorModel>(content);
                var error = new ErrorMessage(model);

                _client.InternalErrorReceived(error);
                return default;
            }
        }
        catch (Exception ex)
        {
            _client.Exception(ex);
            return default;
        }
    }

    // this is all so horrifically bad lmaa
    private async ValueTask<Cookie> LoginAsync()
    {
        var body = JsonConvert.SerializeObject(new { email = _config.Email, password = _config.Password });
        var bodyContent = new StringContent(body, Encoding.UTF8, "application/json");
        var uri = new Uri("https://developer.clashofclans.com/api/login");
        using var response = await _httpClient.PostAsync(uri, bodyContent);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var loginFailure = JsonConvert.DeserializeObject<LoginFailure>(content);
            throw new("Failed to login due to " + loginFailure!.description);
        }

        return _cookieContainer.GetCookies(uri)[0];
    }

    private async ValueTask DeleteOldKeyAsync(Cookie cookie)
    {
        var requestKeysUri = new Uri("https://developer.clashofclans.com/api/apikey/list");
        _cookieContainer.Add(requestKeysUri, cookie);
        using var getKeysResponse = await _httpClient.PostAsync(requestKeysUri, new StringContent("{}", Encoding.UTF8, "application/json"));
        var keysResponseContent = await getKeysResponse.Content.ReadAsStringAsync();

        if (!getKeysResponse.IsSuccessStatusCode)
        {
            throw new("Failed to get keys");
        }

        var keys = JsonConvert.DeserializeObject<ApiKeys>(keysResponseContent);
        var key = keys.keys.FirstOrDefault(key => key.name == "auto-token");
        if (key != null)
        {
            var deleteKeyUri = new Uri("https://developer.clashofclans.com/api/apikey/revoke");
            _cookieContainer.Add(deleteKeyUri, cookie);
            var body = JsonConvert.SerializeObject(new { id = key.id });
            var deleteKeyContent = new StringContent(body, Encoding.UTF8, "application/json");
            using var deleteKeyResponse = await _httpClient.PostAsync(deleteKeyUri, deleteKeyContent);

            if (!deleteKeyResponse.IsSuccessStatusCode)
            {
                throw new("Failed to delete old key");
            }
        }
    }

    private async ValueTask<string> CreateNewKeyAsync(Cookie cookie)
    {
        using var ipResponse = await _httpClient.GetAsync(new Uri("http://ipinfo.io/ip"));
        var currentIp = await ipResponse.Content.ReadAsStringAsync();
        var body = JsonConvert.SerializeObject(new
            { name = "auto-token", description = "ip whitelist is stupid", cidrRanges = new string[]{ currentIp }, scopes = "" });
        var bodyContent = new StringContent(body, Encoding.UTF8, "application/json");
        var createUri = new Uri("https://developer.clashofclans.com/api/apikey/create");
        _cookieContainer.Add(createUri, cookie);
        using var response = await _httpClient.PostAsync(createUri, bodyContent);

        if (!response.IsSuccessStatusCode)
        {
            throw new("Failed to create new key");
        }

        return JsonConvert.DeserializeObject<Key>(await response.Content.ReadAsStringAsync()).key.key;
    }

    private record LoginFailure(string description);

    private record ApiKey(string id, string name);

    private record ApiKeys(ApiKey[] keys);

    private record Key(StupidNestedKey key);

    private record StupidNestedKey(string key);
}