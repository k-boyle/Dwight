﻿using System;
using System.Threading.Tasks;
using Disqord.Bot.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Debugging;

namespace Dwight;

public class Program
{
    public static async Task Main(string[] args)
    {
        using var host = new HostBuilder()
            .ConfigureHostConfiguration(x => x.AddCommandLine(args))
            .ConfigureAppConfiguration((context, config) =>
            {
                var env = context.HostingEnvironment;
                config.AddCommandLine(args)
                    .AddJsonFile("./config/appsettings.json", false, true)
                    .AddJsonFile($"./config/appsettings.{env.EnvironmentName}.json", true, true);
            })
            .UseSerilog((context, serilog) =>
            {
                var env = context.HostingEnvironment!;
                var config = context.Configuration;

                if (env.IsDevelopment())
                {
                    SelfLog.Enable(Console.WriteLine);
                }

                serilog.WriteTo.Console()
                    .ReadFrom.Configuration(config);
            })
            .ConfigureDiscordBot<DwightBot>((context, bot) =>
            {
                bot.Token = context.Configuration["Discord:Token"];
                bot.UseMentionPrefix = true;
            })
            .ConfigureServices((context, collection) =>
            {
                var config = context.Configuration;

                collection.AddDbContext<DwightDbContext>(
                        options => options.UseNpgsql(config.GetConnectionString("Dwight")),
                        optionsLifetime: ServiceLifetime.Singleton
                    )
                    .Configure<TownhallConfiguration>(context.Configuration.GetSection("Clash"))
                    .Configure<PollingConfiguration>(context.Configuration.GetSection("Polling"))
                    .Configure<ClashConfiguration>(context.Configuration.GetSection("Clash"))
                    .AddClashApiClient();
            })
            .Build();

        await using (var scope = host.Services.CreateAsyncScope())
        {
            var services = scope.ServiceProvider;
            var context = services.GetDwightDbContext();
            await context.Database.MigrateAsync();
        }

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