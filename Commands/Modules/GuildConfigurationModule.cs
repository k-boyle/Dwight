using System.Reflection;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Rest;
using Qmmands;
using Qmmands.Delegates;

namespace Dwight
{
    [RequireAuthorGuildPermissions(Permission.Administrator, Group = "perms")]
    [RequireBotOwner(Group = "perms")]
    public class GuildConfigurationModule : DiscordGuildModuleBase
    {
        private readonly DwightDbContext _dbContext;

        [MutateModule]
        public static void MutateModule(ModuleBuilder moduleBuilder)
        {
            var settings = typeof(GuildSettings);
            var properties = settings.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var propertyInfo in properties)
            {
                var attribute = propertyInfo.GetCustomAttribute<MapCommandAttribute>();
                if (attribute == null)
                    continue;

                moduleBuilder.AddCommand(CreateCallback(propertyInfo), commandBuilder =>
                {
                    var trimmedPropertyName = propertyInfo.Name[..^2];
                    commandBuilder.AddAlias(trimmedPropertyName.ToLower())
                        .WithName($"Set{trimmedPropertyName}");
                    var parameterType = attribute.ParameterType;
                    commandBuilder.AddParameter(parameterType, parameterBuilder =>
                    {
                        var trimmedParameterName = parameterType.Name[1..];
                        parameterBuilder.WithName(trimmedParameterName)
                            .WithIsRemainder(true);
                    });
                });
            }
        }

        private static TaskCommandCallbackDelegate CreateCallback(PropertyInfo propertyInfo)
        {
            return async _ =>
            {
                var context = (DiscordGuildCommandContext) _;
                var dbContext = context.Services.GetDwightDbContext();

                var settings = await dbContext.GetOrCreateSettingsAsync(context.GuildId);
                var argument = (ISnowflakeEntity) context.Arguments[0];
                propertyInfo.SetValue(settings, (ulong) argument.Id);
                
                await dbContext.SaveChangesAsync();
                await context.Channel.SendMessageAsync(
                    new()
                    {
                        AllowedMentions = LocalAllowedMentions.ExceptRepliedUser,
                        Content = $"{propertyInfo.Name} has been set to {argument.Id}"
                    }
                );
            };
        }

        public GuildConfigurationModule(DwightDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [Command("clantag")]
        public async Task<CommandResult> SetClanTagAsync(string clanTag)
        {
            clanTag = clanTag.ToUpper();
            
            var settings = await _dbContext.GetOrCreateSettingsAsync(Context.GuildId);
            settings.ClanTag = clanTag;

            return Reply($"Clan tag has been set to {clanTag}");
        }

        [Command("calendarlink")]
        public async Task<CommandResult> SetCalendarLinkAsync(string calendarLink)
        {
            var settings = await _dbContext.GetOrCreateSettingsAsync(Context.GuildId);
            settings.CalendarLink = calendarLink;

            return Reply($"Calendar link has been set to {calendarLink}");
        }

        protected override async ValueTask AfterExecutedAsync()
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}