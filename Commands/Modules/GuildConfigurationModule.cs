using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
    private static readonly Dictionary<string, PropertyInfo> PropertyMapping = new();

    [MutateModule]
    // todo lots of duplication but ohwell
    public static void MutateModule(DiscordBotBase _, ApplicationModuleBuilder moduleBuilder)
    {
        var guildSettings = typeof(GuildSettings);
        var properties = guildSettings.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => new SettingsProperty(property, property.GetCustomAttribute<MapCommandAttribute>()))
            .Where(property => property.Attribute != null)
            .GroupBy(property => property.Attribute!.ParameterType)
            .ToDictionary(property => property.Key, property => property.ToList());

        foreach (var (type, settingsProperties) in properties)
        {
            AddSetCommand(moduleBuilder, type, settingsProperties);
            AddViewCommand(moduleBuilder, type, settingsProperties);
        }
    }

    private static void AddSetCommand(ApplicationModuleBuilder moduleBuilder, Type type, List<SettingsProperty> properties)
    {
        var executionCallback = new DelegateCommandCallback(async _ =>
        {
            var context = (IDiscordApplicationGuildCommandContext)_;
            var dbContext = context.Services.GetDwightDbContext();
        
            var settings = await dbContext.GetOrCreateSettingsAsync(context.GuildId);

            var propertyString = context.Arguments![context.Command!.Parameters[0]];
            var propertyInfo = PropertyMapping[(string) propertyString!];

            var argument = context.Arguments[context.Command.Parameters[1]]!;

            switch (argument)
            {
                case ISnowflakeEntity snowflakeEntity:
                    propertyInfo.SetValue(settings, (ulong)snowflakeEntity.Id);
                    break;
                default:
                    propertyInfo.SetValue(settings, argument);
                    break;
            }

            await dbContext.SaveChangesAsync();

            var setResponse = new LocalInteractionMessageResponse()
                .WithContent($"{propertyInfo.Name} has been set to {argument}");
            return new DiscordInteractionResponseCommandResult(context, setResponse);
        });
        
        var commandAlias = type.Name;
        if (type.IsInterface) 
            commandAlias = commandAlias[1..];

        var setModule = moduleBuilder.Submodules[0];

        var setCommandAlias = Spacify(commandAlias, '-').ToLower();

        var setCommandBuilder = new ApplicationCommandBuilder(setModule, executionCallback)
        {
            Type = ApplicationCommandType.Slash,
            Name = Spacify(commandAlias, ' '),
            Alias = setCommandAlias,
        };

        var propertyParameter = new ApplicationParameterBuilder(setCommandBuilder, typeof(string))
        {
            Name = "Property",
            Description = "The property to set"
        };

        foreach (var settingsProperty in properties)
        {
            var choice = settingsProperty.Property.Name;
            if (choice.EndsWith("Id"))
                choice = choice[..^2];

            PropertyMapping[choice] = settingsProperty.Property;

            var choiceAttribute = new ChoiceAttribute(choice, choice);
            propertyParameter.CustomAttributes.Add(choiceAttribute);
        }

        setCommandBuilder.Parameters.Add(propertyParameter);

        var valueParameter = new ApplicationParameterBuilder(setCommandBuilder, type)
        {
            Name = "Value",
            Description = "The value to set the property to",
        };

        if (type == typeof(IInteractionChannel))
        {
            var channelTypeAttribute = new ChannelTypesAttribute(ChannelType.Text);
            valueParameter.CustomAttributes.Add(channelTypeAttribute);
        }

        setCommandBuilder.Parameters.Add(valueParameter);

        setModule.Commands.Add(setCommandBuilder);
    }

    private static void AddViewCommand(ApplicationModuleBuilder moduleBuilder, Type type, List<SettingsProperty> properties)
    {
        
        var executionCallback = new DelegateCommandCallback(async _ =>
        {
            var context = (IDiscordApplicationGuildCommandContext)_;
            var dbContext = context.Services.GetDwightDbContext();
        
            var settings = await dbContext.GetOrCreateSettingsAsync(context.GuildId);

            var propertyString = context.Arguments![context.Command!.Parameters[0]];
            var propertyInfo = PropertyMapping[(string) propertyString!];

            var currentValue = propertyInfo.GetValue(settings);

            var formattedValue = currentValue switch
            {
                ulong and 0 or null => Markdown.Code("unset"),
                ulong role when typeof(IRole).IsAssignableFrom(type) => Mention.Role(role),
                ulong channel when typeof(IChannel).IsAssignableFrom(type) => Mention.Channel(channel),
                ulong user when typeof(IUser).IsAssignableFrom(type) => Mention.User(user),
                _ => $"\"{currentValue}\""
            };
            
            var setResponse = new LocalInteractionMessageResponse()
                .WithContent($"{propertyInfo.Name} has been set to {formattedValue}");
            return new DiscordInteractionResponseCommandResult(context, setResponse);
        });
        
        var commandAlias = type.Name;
        if (type.IsInterface) 
            commandAlias = commandAlias[1..];

        var viewModule = moduleBuilder.Submodules[1];

        var viewCommandAlias = Spacify(commandAlias, '-').ToLower();

        var viewCommandBuilder = new ApplicationCommandBuilder(viewModule, executionCallback)
        {
            Type = ApplicationCommandType.Slash,
            Name = Spacify(commandAlias, ' '),
            Alias = viewCommandAlias,
        };

        var propertyParameter = new ApplicationParameterBuilder(viewCommandBuilder, typeof(string))
        {
            Name = "Property",
            Description = "The property to view"
        };

        foreach (var settingsProperty in properties)
        {
            var choice = settingsProperty.Property.Name;
            if (choice.EndsWith("Id"))
                choice = choice[..^2];

            PropertyMapping[choice] = settingsProperty.Property;

            var choiceAttribute = new ChoiceAttribute(choice, choice);
            propertyParameter.CustomAttributes.Add(choiceAttribute);
        }

        viewCommandBuilder.Parameters.Add(propertyParameter);
        viewModule.Commands.Add(viewCommandBuilder);
    }

    [SlashGroup("set")]
    public class SetCommands : GuildConfigurationModule;
    
    [SlashGroup("view")]
    public class ViewCommands : GuildConfigurationModule;

    private record SettingsProperty(PropertyInfo Property, MapCommandAttribute? Attribute);

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
}