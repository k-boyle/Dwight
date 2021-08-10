using System.Threading;
using System.Threading.Tasks;
using Disqord.Bot.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dwight.Services
{
    public class StartupService : DiscordBotService
    {
        public override int Priority => int.MaxValue;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await using var scope = Bot.Services.CreateAsyncScope();
            var services = scope.ServiceProvider;
            var context = services.GetDwightDbContext();
            await context.Database.MigrateAsync(stoppingToken);
        }
    }
}