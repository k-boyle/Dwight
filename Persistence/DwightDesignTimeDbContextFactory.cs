using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Yaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dwight
{
    public class DwightDesignTimeDbContextFactory : IDesignTimeDbContextFactory<DwightDbContext>
    {
        private const string CONFIG = "./config.yml";
        
        public DwightDbContext CreateDbContext(string[] args)
        {
            return new HostBuilder()
                .ConfigureAppConfiguration(x => x.AddYamlFile(CONFIG))
                .ConfigureServices((context, collection) => collection.AddDbContext<DwightDbContext>(options => options.UseNpgsql(context.Configuration.GetConnectionString("Dwight"))))
                .Build()
                .Services
                .CreateScope()
                .ServiceProvider
                .GetService<DwightDbContext>();
        }
    }
}