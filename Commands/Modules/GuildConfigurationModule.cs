using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Bot.Commands.Interaction;
using Qmmands;

namespace Dwight;

[RequireAuthorPermissions(Permissions.Administrator, Group = "perms")]
[RequireBotOwner(Group = "perms")]
public class GuildConfigurationModule : DiscordApplicationGuildModuleBase
{
    private readonly DwightDbContext _dbContext;

    [MutateModule]
    public static void MutateModule(DiscordBotBase _, ApplicationModuleBuilder moduleBuilder)
    {
        var guildSettings = typeof(GuildSettings);
        var properties = guildSettings.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var propertyInfo in properties)
        {
            var attribute = propertyInfo.GetCustomAttribute<MapCommandAttribute>();
            if (attribute == null)
                continue;

            AddSetCommand(moduleBuilder, propertyInfo, attribute);
            AddViewCommand(moduleBuilder, propertyInfo);
        }
    }

    // todo duplicate code
    private static void AddSetCommand(ApplicationModuleBuilder moduleBuilder, PropertyInfo propertyInfo, MapCommandAttribute attribute)
    {
        var callback = new DelegateCommandCallback(async _ =>
        {
            var context = (IDiscordApplicationCommandContext)_;
            var dbContext = context.Services.GetDwightDbContext();

            var settings = await dbContext.GetOrCreateSettingsAsync(context.GuildId!.Value.RawValue);
            var argument = (ISnowflakeEntity)context.Arguments![context.Command!.Parameters[0]]!;
            propertyInfo.SetValue(settings, (ulong)argument.Id);

            await dbContext.SaveChangesAsync();

            var setResponse = new LocalInteractionMessageResponse().WithContent($"{propertyInfo.Name} has been set to {argument.Id}");
            return new DiscordInteractionResponseCommandResult(context, setResponse);
        });

        var trimmedPropertyName = propertyInfo.Name[..^2];
        var name = Spacify(trimmedPropertyName, ' ');
        var setModule = moduleBuilder.Submodules[0];
        var commandBuilder = new ApplicationCommandBuilder(setModule, callback)
        {
            Name = name,
            Alias = Spacify(trimmedPropertyName, '-').ToLower(),
            Description = $"Sets the {name}"
        };

        var parameterType = attribute.ParameterType;
        var parameter = new ApplicationParameterBuilder(commandBuilder, parameterType)
        {
            Name = parameterType.Name[1..]
        };

        commandBuilder.Parameters.Add(parameter);

        setModule.Commands.Add(commandBuilder);
    }

    private static void AddViewCommand(ApplicationModuleBuilder moduleBuilder, PropertyInfo propertyInfo)
    {
        var callback = new DelegateCommandCallback(async _ =>
        {
            var context = (IDiscordApplicationCommandContext)_;
            var dbContext = context.Services.GetDwightDbContext();

            var settings = await dbContext.GetOrCreateSettingsAsync(context.GuildId!.Value.RawValue);
            var value = propertyInfo.GetValue(settings);

            var setResponse = new LocalInteractionMessageResponse().WithContent($"{propertyInfo.Name} has been set to \"{value}\"");
            return new DiscordInteractionResponseCommandResult(context, setResponse);
        });

        var trimmedPropertyName = propertyInfo.Name[..^2];
        var name = Spacify(trimmedPropertyName, ' ');
        var viewModule = moduleBuilder.Submodules[1];
        var commandBuilder = new ApplicationCommandBuilder(viewModule, callback)
        {
            Name = name,
            Alias = Spacify(trimmedPropertyName, '-').ToLower(),
            Description = $"Views the {name}"
        };

        viewModule.Commands.Add(commandBuilder);
    }

    private static string Spacify(string str, char separator)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < str.Length; i++)
        {
            if (i != 00 && char.IsUpper(str[i]))
            {
                builder.Append(separator);
            }

            builder.Append(str[i]);
        }

        return builder.ToString();
    }

    public GuildConfigurationModule(DwightDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [SlashGroup("set")]
    public class SetCommands : GuildConfigurationModule
    {
        public SetCommands(DwightDbContext dbContext) : base(dbContext)
        {
        }

        [SlashCommand("clan-tag")]
        [Description("Sets the clan tag")]
        public async ValueTask<IResult> SetClanTagAsync(string clanTag)
        {
            clanTag = clanTag.ToUpper();

            var settings = await _dbContext.GetOrCreateSettingsAsync(Context.GuildId.RawValue);
            settings.ClanTag = clanTag;

            return Response($"Clan tag has been set to {clanTag}");
        }

        public override async ValueTask OnAfterExecuted()
        {
            await _dbContext.SaveChangesAsync();
        }
    }

    [SlashGroup("view")]
    public class ViewCommands : GuildConfigurationModule
    {
        public ViewCommands(DwightDbContext dbContext) : base(dbContext)
        {
        }

        [SlashCommand("clan-tag")]
        [Description("Views the clan tag")]
        public async ValueTask<IResult> ViewClanTagAsync()
        {
            var settings = await _dbContext.GetOrCreateSettingsAsync(Context.GuildId.RawValue);
            return Response($"Clan tag has been set to \"{settings.ClanTag}\"");
        }

        public override async ValueTask OnAfterExecuted()
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}