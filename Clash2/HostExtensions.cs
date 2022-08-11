using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dwight;

public static class HostExtensions
{
    public static IHostBuilder AddClashClient(this IHostBuilder builder)
    {
        return builder.ConfigureServices(collection =>
        {
            collection.AddHttpClient(nameof(ClashDevClient), client =>
            {
                client.BaseAddress = new("https://developer.clashofclans.com/api/");
                client.DefaultRequestHeaders.Accept.Add(new("application/json"));
            });

            collection.AddSingleton<ClashDevClient>();

            collection.AddHttpClient(nameof(ClashClient2), client => { });
        });
    }
}