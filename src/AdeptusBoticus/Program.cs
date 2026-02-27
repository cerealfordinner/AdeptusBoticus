using System.ServiceModel.Syndication;
using System.Xml;
using AdeptusBoticus.Data;
using AdeptusBoticus.Extensions;
using AdeptusBoticus.Models;
using AdeptusBoticus.Services;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace AdeptusBoticus;

public sealed class Program
{
    private static IServiceProvider _serviceProvider = null!;
    private static System.Timers.Timer? _rssCheckTimer;
    private static readonly ManualResetEvent _shutdownEvent = new(false);

    public static async Task Main(string[] args)
    {
        Console.CancelKeyPress += OnShutdown;
        DotNetEnv.Env.Load();

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            var dataService = _serviceProvider.GetRequiredService<IDataService>();
            dataService.InitializeCategoryTimestamps();

            var discordBot = _serviceProvider.GetRequiredService<IDiscordBot>();
            await discordBot.ConnectAsync();

            var config = _serviceProvider.GetRequiredService<BotConfiguration>();
            InitializeRssFeedChecker(config);

            Log.Information("Bot started successfully");
            _shutdownEvent.WaitOne();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            return;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var config = LoadConfiguration();
        services.AddSingleton(config);

        var loggerFactory = new SerilogLoggerFactory(Log.Logger);
        services.AddSingleton<ILoggerFactory>(loggerFactory);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

        services.AddSingleton<IDataService>(sp =>
            new DataService(
                config.DataFilePath,
                sp.GetRequiredService<ILogger<DataService>>()));

        services.AddSingleton<IDiscordBot>(sp =>
            new DiscordBot(
                config.DiscordToken,
                sp.GetRequiredService<ILogger<DiscordBot>>()));

        services.AddHttpClient<IWarComArticleReader, WarComArticleReader>();
        services.AddSingleton<IWarComArticleService, WarComArticleService>();
    }

    private static void OnShutdown(object? sender, ConsoleCancelEventArgs e)
    {
        Log.Information("Shutting down...");
        _rssCheckTimer?.Stop();
        _rssCheckTimer?.Dispose();
        _shutdownEvent.Set();
    }

    private static string GetRequiredEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Required environment variable '{name}' is not set.");
        }
        return value;
    }

    private static ulong GetChannelIdFromEnv(string name)
    {
        var value = GetRequiredEnvironmentVariable(name);
        if (!ulong.TryParse(value, out var channelId))
        {
            throw new InvalidOperationException($"Environment variable '{name}' must be a valid ulong, got: '{value}'");
        }
        return channelId;
    }

    private static BotConfiguration LoadConfiguration()
    {
        return new BotConfiguration
        {
            DiscordToken = GetRequiredEnvironmentVariable("DISCORD_TOKEN"),
            RssCheckIntervalMs = int.TryParse(Environment.GetEnvironmentVariable("RSS_CHECK_INTERVAL_MS"), out var interval) ? interval : 300000,
            FeedUrl = Environment.GetEnvironmentVariable("FEED_URL") ?? "https://www.warhammer-community.com/api/search/news/",
            DataFilePath = Environment.GetEnvironmentVariable("DATA_FILE_PATH") ?? "./data/trackers.json",
            Channels = LoadChannelConfigs()
        };
    }

    private static List<ChannelConfig> LoadChannelConfigs()
    {
        return
        [
            new ChannelConfig
            {
                ChannelName = ChannelNameEnum.WH40K,
                ChannelId = GetChannelIdFromEnv("WH40K_ID"),
                Categories = ["Warhammer 40000", "Warhammer 40,000", "40k", "Kill Team"]
            },

            new ChannelConfig
            {
                ChannelName = ChannelNameEnum.AOS,
                ChannelId = GetChannelIdFromEnv("AOS_ID"),
                Categories = ["Warhammer Age of Sigmar", "Old World", "Warhammer: The Old World", "Old World Almanack", "Arcane Journal", "AoS"]
            },

            new ChannelConfig
            {
                ChannelName = ChannelNameEnum.HH,
                ChannelId = GetChannelIdFromEnv("HH_ID"),
                Categories = ["The Horus Heresy", "Warhammer: The Horus Heresy", "The Horus Heresy News"]
            },

            new ChannelConfig
            {
                ChannelName = ChannelNameEnum.BB,
                ChannelId = GetChannelIdFromEnv("BB_ID"),
                Categories = ["Blood Bowl"]
            }
        ];
    }

    private static void InitializeRssFeedChecker(BotConfiguration config)
    {
        _rssCheckTimer = new System.Timers.Timer(config.RssCheckIntervalMs);
        _rssCheckTimer.Elapsed += OnTimerElapsed;
        _rssCheckTimer.AutoReset = true;
        _rssCheckTimer.Enabled = true;

        Log.Information("RSS checker initialized with interval {Interval}ms", config.RssCheckIntervalMs);
        
        var rssService = _serviceProvider.GetRequiredService<IWarComArticleService>();
        _ = Task.Run(() => rssService.CheckArticlesAsync());
    }

    private static async void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            var rssService = _serviceProvider.GetRequiredService<IWarComArticleService>();
            await rssService.CheckArticlesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking RSS feed");
        }
    }
}
