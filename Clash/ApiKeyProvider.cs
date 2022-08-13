using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dwight;

public class ApiKeyProvider
{
    private readonly ClashDevClient _clashDevClient;
    private readonly ILogger<ApiKeyProvider> _logger;
    private readonly SemaphoreSlim _semaphoreSlim;
    private readonly ClashConfiguration _clashConfiguration;

    private string? _currentKey;

    public ApiKeyProvider(ClashDevClient clashDevClient, ILogger<ApiKeyProvider> logger, IOptions<ClashConfiguration> clashConfiguration)
    {
        _clashDevClient = clashDevClient;
        _logger = logger;
        _semaphoreSlim = new(1);
        _clashConfiguration = clashConfiguration.Value;
    }

    public async ValueTask<string> GetKeyAsync(bool allowCache, CancellationToken cancellationToken)
    {
        if (allowCache && _currentKey != null)
            return _currentKey;

        try
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);
            return await GetTokenAsyncInternal(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error whilst trying to get an api token");
            throw;
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    private async Task<string> GetTokenAsyncInternal(CancellationToken cancellationToken)
    {
        if (_currentKey != null)
            return _currentKey;

        _logger.LogInformation("Getting new token for api");

        var cookie = await _clashDevClient.LoginAsync(new(_clashConfiguration.Email, _clashConfiguration.Password), cancellationToken);
        var apiKeys = await _clashDevClient.GetKeysAsync(cookie, cancellationToken);

        var oldKey = apiKeys.keys.FirstOrDefault(key => key.name == "auto-token");
        if (oldKey != null)
        {
            _logger.LogInformation("Found old auto key, revoking");
            await _clashDevClient.RevokeKeyAsync(new(oldKey.id), cookie, cancellationToken);
        }

        var newKey = await _clashDevClient.CreateKeyAsync(new("auto-token", "ip whitelist is dumb"), cookie, cancellationToken);
        return _currentKey = newKey;
    }
}