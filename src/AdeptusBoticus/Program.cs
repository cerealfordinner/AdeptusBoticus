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
            FeedUrl = Environment.GetEnvironmentVariable("FEED_URL") ?? "https://www.warhammer-community.com/feed/",
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
                Categories = ["Warhammer 40000", "40k", "Kill Team"]
            },

            new ChannelConfig
            {
                ChannelName = ChannelNameEnum.AOS,
                ChannelId = GetChannelIdFromEnv("AOS_ID"),
                Categories = ["Warhammer Age of Sigmar", "Old World", "Old World Almanack", "Arcane Journal", "AoS"]
            },

            new ChannelConfig
            {
                ChannelName = ChannelNameEnum.HH,
                ChannelId = GetChannelIdFromEnv("HH_ID"),
                Categories = ["The Horus Heresy", "The Horus Heresy News"]
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
        _ = Task.Run(() => CheckRssFeedAsync(config));
    }

    private static async void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            var config = _serviceProvider.GetRequiredService<BotConfiguration>();
            await CheckRssFeedAsync(config);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking RSS feed");
        }
    }

    private static async Task CheckRssFeedAsync(BotConfiguration config)
    {
        Log.Debug("Checking Warhammer Community RSS feed...");

        try
        {
            using var reader = XmlReader.Create(config.FeedUrl);
            var feed = SyndicationFeed.Load(reader);

            var dataService = _serviceProvider.GetRequiredService<IDataService>();
            var discordBot = _serviceProvider.GetRequiredService<IDiscordBot>();

            foreach (var channelConfig in config.Channels)
            {
                var item = feed.Items
                    .Where(item => channelConfig.Categories.Any(category => item.Categories
                        .Any(feedCategory => feedCategory.Name
                            .Equals(category, StringComparison.CurrentCultureIgnoreCase))))
                    .MaxBy(item => item.PublishDate);

                if (item != null)
                {
                    var itemDateTime = item.PublishDate.UtcDateTime;
                    var categoryTracker = dataService.GetTracker(channelConfig.ChannelName);

                    if (categoryTracker == null || itemDateTime > categoryTracker.LastPostedItemTimeStamp)
                    {
                        var channel = await discordBot.GetChannelAsync(channelConfig.ChannelId);

                        var imageThumbnailExtension = item.ElementExtensions
                            .FirstOrDefault(ext => ext.OuterName == "image_medium");

                        var thumbnailUrl = imageThumbnailExtension?.GetObject<string>();

                        var embed = new DiscordEmbedBuilder
                        {
                            Title = item.Title.Text,
                            Url = item.Links?.FirstOrDefault()?.Uri.ToString(),
                            Description = item.Summary.Text.StripHtmlTags(),
                            ImageUrl = thumbnailUrl
                        };

                        await channel.SendMessageAsync(embed);
                        Log.Information("Posted new item to channel {ChannelName}: {ItemTitle}", channel.Name, item.Title.Text);

                        dataService.UpdateLastPostedItemTimestamp(channelConfig.ChannelName, item.PublishDate.UtcDateTime);
                    }
                }
            }
            Log.Debug("RSS check complete.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to check RSS feed");
            throw;
        }
    }
}
