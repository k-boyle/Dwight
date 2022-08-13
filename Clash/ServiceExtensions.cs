using Microsoft.Extensions.DependencyInjection;

namespace Dwight;

public static class ServiceExtensions
{
    public static IServiceCollection AddClashApiClient(this IServiceCollection collection)
    {
        collection.AddHttpClient<ClashDevClient>(client =>
        {
            client.BaseAddress = new("https://developer.clashofclans.com/");
            client.DefaultRequestHeaders.Accept.Add(new("application/json"));
        });

        collection.AddHttpClient<ClashApiClient>(client =>
            {
                client.BaseAddress = new("https://api.clashofclans.com/");
                client.DefaultRequestHeaders.Accept.Add(new("application/json"));
            })
            .AddHttpMessageHandler<ApiKeyProviderHandler>();

        return collection.AddSingleton<ApiKeyProvider>()
            .AddSingleton<ApiKeyProviderHandler>();
    }
}