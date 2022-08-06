using System;
using System.Threading.Tasks;
using ClashWrapper;
using Disqord.Bot.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Debugging;
using Serilog.Formatting.Elasticsearch;

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

                var elastic = config.GetSection("Elastic")!;

                serilog.WriteTo.Console()
                    .WriteTo.Elasticsearch(new(new Uri(elastic["Host"]))
                    {
                        ModifyConnectionSettings = connection => connection.ApiKeyAuthentication(new(elastic["ApiKey"])),
                        TypeName = null,
                        InlineFields = true,
                        CustomFormatter = new ElasticsearchJsonFormatter()
                    })
                    .Enrich.WithProperty("app", env.ApplicationName)
                    .Enrich.WithProperty("env", env.EnvironmentName)
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
                    .AddSingleton(new ClashClientConfig { Email = config["Clash:Email"], Password = config["Clash:Password"] })
                    .AddSingleton<ClashClient>()
                    .AddHttpClient()
                    .Configure<TownhallConfiguration>(context.Configuration.GetSection("Clash"))
                    .Configure<PollingConfiguration>(context.Configuration.GetSection("Polling"));
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