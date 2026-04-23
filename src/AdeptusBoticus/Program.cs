using AdeptusBoticus.Data;
using AdeptusBoticus.Models;
using AdeptusBoticus.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace AdeptusBoticus;

public sealed class Program
{
    private static IServiceProvider _serviceProvider = null!;
    private static System.Timers.Timer? _pollingTimer;
    private static readonly ManualResetEvent _shutdownEvent = new(false);

    public static async Task Main(string[] args)
    {
        // Configure Serilog to write to console
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/adeptusboticus-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Console.CancelKeyPress += OnShutdown;
        DotNetEnv.Env.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env"));

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            var dataService = _serviceProvider.GetRequiredService<IDataService>();
            dataService.InitializeCategoryTrackers();

            var discordBot = _serviceProvider.GetRequiredService<IDiscordBot>();
            await discordBot.ConnectAsync();

            var config = _serviceProvider.GetRequiredService<BotConfiguration>();
            InitializePolling(config);

            // Perform initial check immediately
            var rssService = _serviceProvider.GetRequiredService<IWarComArticleService>();
            await rssService.CheckArticlesAsync();

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
        _pollingTimer?.Stop();
        _pollingTimer?.Dispose();
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
            PollingIntervalMs = int.TryParse(Environment.GetEnvironmentVariable("POLLING_INTERVAL_MS"), out var interval) ? interval : 300000,
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

    private static void InitializePolling(BotConfiguration config)
    {
        Log.Information("Initializing article polling with interval {Interval}ms", config.PollingIntervalMs);
        _pollingTimer = new System.Timers.Timer(config.PollingIntervalMs);
        _pollingTimer.Elapsed += OnTimerElapsed;
        _pollingTimer.AutoReset = true;
        _pollingTimer.Enabled = true;

        Log.Information("Polling timer created. Enabled: {Enabled}, Interval: {Interval}ms, AutoReset: {AutoReset}",
            _pollingTimer.Enabled, _pollingTimer.Interval, _pollingTimer.AutoReset);
    }

    private static async void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            Log.Information("Article polling cycle started (Timer fired at {Time})", DateTime.UtcNow);

            var rssService = _serviceProvider.GetRequiredService<IWarComArticleService>();
            await rssService.CheckArticlesAsync();

            Log.Information("Article polling cycle completed successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Article polling cycle failed - timer may stop if exception escapes");
            // Re-throw to let the timer know something went wrong?
            // Actually, don't re-throw - we want to keep the timer alive
        }
        catch
        {
            Log.Error("Unknown error in timer elapsed event");
        }
    }
}
