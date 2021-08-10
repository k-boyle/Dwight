using System;
using Microsoft.Extensions.DependencyInjection;

namespace Dwight
{
    public static class Extensions
    {
        public static DwightDbContext GetDwightDbContext(this IServiceProvider services)
            => services.GetRequiredService<DwightDbContext>();
    }
}