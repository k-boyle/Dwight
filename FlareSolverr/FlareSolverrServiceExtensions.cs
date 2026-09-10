using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dwight;

public static class FlareSolverrServiceExtensions
{
    public static IServiceCollection AddFlareSolverrClient(this IServiceCollection collection)
    {
        collection.AddHttpClient<FlareSolverrClient>((provider, client) =>
        {
            var configuration = provider.GetRequiredService<IOptions<FlareSolverrConfiguration>>().Value;
            client.BaseAddress = new(configuration.BaseUrl);
        });

        return collection;
    }
}
