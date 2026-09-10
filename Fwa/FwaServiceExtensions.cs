using Microsoft.Extensions.DependencyInjection;

namespace Dwight;

public static class FwaServiceExtensions
{
    public static IServiceCollection AddFwaClients(this IServiceCollection collection)
    {
        return collection.AddFlareSolverrClient()
            .AddTransient<FwaPointsClient>()
            .AddTransient<FwaMemberClient>();
    }
}
