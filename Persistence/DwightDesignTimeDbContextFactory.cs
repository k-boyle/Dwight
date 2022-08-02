using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dwight;

public class DwightDesignTimeDbContextFactory : IDesignTimeDbContextFactory<DwightDbContext>
{
    public DwightDbContext CreateDbContext(string[] args)
    {
        return new HostBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddCommandLine(args)
                    .AddJsonFile($"./config/appsettings.Development.json");
            })
            .ConfigureServices((context, collection)
                => collection.AddDbContext<DwightDbContext>(options => options.UseNpgsql(context.Configuration.GetConnectionString("Dwight"))))
            .Build()
            .Services
            .CreateScope()
            .ServiceProvider
            .GetRequiredService<DwightDbContext>();
    }
}