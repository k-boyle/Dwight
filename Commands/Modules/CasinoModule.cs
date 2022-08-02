using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;

namespace Dwight;

[RequireBotOwner]
public class CasinoModule : DiscordApplicationGuildModuleBase
{
    private readonly DwightDbContext _dbContext;

    public CasinoModule(DwightDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // [Command("sql")]
    // public async ValueTask<CommandResult> ExecuteSqlAsync([Remainder] string statement)
    // {
    //     await using var connection = _dbContext.Database.GetDbConnection();
    //     await connection.OpenAsync();
    //
    //     await using var command = connection.CreateCommand();
    //     command.CommandText = statement;
    //     command.CommandType = CommandType.Text;
    //
    //     await using var reader = await command.ExecuteReaderAsync();
    //
    //     return Reply(string.Join('\n', rows.Select(row => string.Join(", ", row))));
    // }
}