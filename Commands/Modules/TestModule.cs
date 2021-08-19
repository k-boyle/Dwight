using Disqord.Bot;
using Qmmands;

namespace Dwight
{
    public class TestModule : DiscordGuildModuleBase
    {
        private readonly DwightDbContext _dbContext;

        public TestModule(DwightDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [Command("ping")]
        public CommandResult Ping()
            => Reply("pong");
    }
}