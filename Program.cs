using System;
using System.Net.Http;
using ClashWrapper;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Yaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Dwight
{
    public class Program
    {
        private const string CONFIG = "./config.yml";

        public static void Main(string[] args)
        {
            using var host = new HostBuilder()
                .ConfigureHostConfiguration(x => x.AddCommandLine(args).AddYamlFile(CONFIG))
                .ConfigureAppConfiguration(x => x.AddCommandLine(args).AddYamlFile(CONFIG))
                .UseSerilog((context, configuration) =>
                {
                    configuration.ReadFrom.Configuration(context.Configuration)
                        .Enrich.With<ClassNameEnricher>()
                        .WriteTo.Console(outputTemplate: LoggingTemplate.CONSOLE, theme: LoggingConsoleTheme.Instance)
                        .WriteTo.File("./logs/log-.txt", outputTemplate: LoggingTemplate.FILE, rollingInterval: RollingInterval.Day);
                })
                .ConfigureDiscordBot<DwightBot>((context, bot) =>
                {
                    bot.Token = context.Configuration["Discord:Token"];
                    bot.UseMentionPrefix = true;
                    bot.Prefixes = new[] { "dwight", "dwight,", "d" };
                    bot.Intents = GatewayIntents.Recommended + GatewayIntent.Presences;
                })
                .ConfigureServices((context, collection) =>
                {
                    collection.AddDbContext<DwightDbContext>(
                            options => options.UseNpgsql(context.Configuration.GetConnectionString("Dwight")),
                            optionsLifetime: ServiceLifetime.Singleton
                        )
                        .AddSingleton<EspeonScheduler>()
                        .AddSingleton(new ClashClient(new() { Token = context.Configuration["Clash:Token"] }))
                        .AddSingleton<HttpClient>()
                        .Configure<TownhallConfiguration>(context.Configuration.GetSection("Clash"))
                        .Configure<PollingConfiguration>(context.Configuration.GetSection("Polling"));
                })
                .Build();

            try
            {
                host.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}