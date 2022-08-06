using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dwight;

public class TokenProvider
{
    private readonly ClashDevClient _clashDevClient;
    private readonly ILogger<TokenProvider> _logger;
    private readonly SemaphoreSlim _semaphoreSlim;
    private readonly ClashConfiguration _clashConfiguration;

    private string? _currentToken;
    private DateTimeOffset _expiresAt;

    public TokenProvider(ClashDevClient clashDevClient, ILogger<TokenProvider> logger, IOptions<ClashConfiguration> clashConfiguration)
    {
        _clashDevClient = clashDevClient;
        _logger = logger;
        _semaphoreSlim = new(1);
        _clashConfiguration = clashConfiguration.Value;
    }

    public async ValueTask<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        await _semaphoreSlim.WaitAsync(cancellationToken);

        if (_currentToken != null && _expiresAt < DateTimeOffset.UtcNow.AddMinutes(-5))
        {
            _semaphoreSlim.Release();
            return _currentToken;
        }

        _logger.LogInformation("Getting new token for api");
        
        var login = await _clashDevClient.LoginAsync(new(_clashConfiguration.Email, _clashConfiguration.Password), cancellationToken);
        if (login == null)
        {
            _logger.LogError("Failed to log in");

            _semaphoreSlim.Release();
            throw new("Failed to login");
        }

        _currentToken = login.TemporaryApiToken;
        _expiresAt = DateTimeOffset.UtcNow.AddSeconds(login.SessionExpiresInSeconds);
        _semaphoreSlim.Release();

        return _currentToken;
    }
}