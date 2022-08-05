using System;
using System.Threading.Tasks;
using ClashWrapper;
using ClashWrapper.Entities.War;

namespace Dwight;

public class WarReminder
{
    private readonly ClashClient _clashClient;
    private readonly Func<ValueTask<GuildSettings>> _guildSettingsSupplier;
    private readonly Func<Task<bool>> _maintenanceDelay;

    public async Task RunAsync()
    {
        var settings = await _guildSettingsSupplier();
        if (settings.ClanTag == null)
            return;

        var currentWar = await _clashClient.GetCurrentWarAsync(settings.ClanTag);
        if (currentWar == null || currentWar.State is WarState.Default or WarState.Ended)
            return;
        
        
    }
}