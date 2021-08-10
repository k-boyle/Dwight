﻿using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Dwight
{
    [Group("data")]
    public class DataModule : DiscordGuildModuleBase
    {
        private readonly DwightDbContext _dbContext;

        private const string DUMP_QUERIES_SQL = @"
SELECT table_name, 'SELECT json_agg(t) FROM (SELECT * FROM ' || table_name || ' WHERE {0}) t;'
FROM information_schema.tables
WHERE table_schema='public'
AND table_name!='__EFMigrationsHistory';
";

        public DataModule(DwightDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [Command("guild")]
        [RequireBotOwner]
        public async ValueTask<CommandResult> DumpDbAsync()
        {
            await using var connection = _dbContext.Database.GetDbConnection();
            await connection.OpenAsync();

            var tablesAndQueries = new List<(string, string)>();
            await using (var generateQueriesCommand = connection.CreateCommand())
            {
                generateQueriesCommand.CommandText = DUMP_QUERIES_SQL;
                generateQueriesCommand.CommandType = CommandType.Text;

                await using var queryReader = await generateQueriesCommand.ExecuteReaderAsync();

                while (await queryReader.ReadAsync())
                {
                    var tableName = queryReader.GetString(0);
                    var jsonDumpQuery = queryReader.GetString(1);

                    tablesAndQueries.Add((tableName, jsonDumpQuery));
                }
            }

            var attachments = new List<LocalAttachment>();
            foreach (var tableAndQuery in tablesAndQueries)
            {
                var (tableName, query) = tableAndQuery;
                
                await using var dumpJsonCommand = connection.CreateCommand();

                dumpJsonCommand.CommandText = string.Format(query, $"\"GuildId\"={Context.GuildId.RawValue}");
                dumpJsonCommand.CommandType = CommandType.Text;

                await using var jsonReader = await dumpJsonCommand.ExecuteReaderAsync();

                while (await jsonReader.ReadAsync())
                {
                    var value = jsonReader.GetValue(0);
                    if (value is not string json)
                        continue;

                    var parsedJson = JsonSerializer.Deserialize<JsonElement>(json);
                    var prettyJson = JsonSerializer.SerializeToUtf8Bytes(parsedJson, new JsonSerializerOptions { WriteIndented = true });
                    var attachment = LocalAttachment.Bytes(prettyJson, tableName + ".json");
                    attachments.Add(attachment);
                }
            }

            return attachments.Count == 0
                ? Reply("No data to dump")
                : Reply(new LocalMessage { Content = "Don't tell the cops", Attachments = attachments });
        }
    }
}