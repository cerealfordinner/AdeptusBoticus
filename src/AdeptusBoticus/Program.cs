using System.ServiceModel.Syndication;
using System.Xml;
using AdeptusBoticus.Data;
using AdeptusBoticus.Extensions;
using AdeptusBoticus.Models;
using DSharpPlus;
using DSharpPlus.Entities;
using MongoDB.Driver;
using Timer = System.Timers.Timer;

namespace AdeptusBoticus;

public sealed class Program
{
    private static DiscordClient? _client;
    private static List<ChannelConfig>? _channelConfigs;
    private static Timer? _rssCheckTimer;

    private static DataService? _dataService;
    private static readonly string _feedUrl = "https://www.warhammer-community.com/feed/";

    private static async Task Main(string[] args)
    {
        var botToken = Environment.GetEnvironmentVariable("DISCORD_TOKEN");

        _dataService = new DataService();
        _dataService.InitializeCategoryTimestamps();

        _client = new DiscordClient(new DiscordConfiguration
        {
            Token = botToken,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged
        });

        try
        {
            await _client.ConnectAsync();
            Console.WriteLine("Bot connected to Discord!");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        InitializeChannelConfigs();
        InitializeRssFeedChecker();

        await Task.Delay(-1);
    }

    private static void InitializeChannelConfigs()
    {
        _channelConfigs =
        [
            new ChannelConfig
            {
                ChannelName = ChannelNameEnum.WH40K,
                ChannelId = ulong.Parse(Environment.GetEnvironmentVariable("WH40K_ID") ?? string.Empty),
                Categories = ["Warhammer 40000", "40k", "Kill Team"]
            },

            new ChannelConfig
            {
                ChannelName = ChannelNameEnum.AOS,
                ChannelId = ulong.Parse(Environment.GetEnvironmentVariable("AOS_ID") ?? string.Empty),
                Categories = ["Warhammer Age of Sigmar", "Old World", "Old World Almanack", "Arcane Journal", "AoS"]
            },

            new ChannelConfig
            {
                ChannelName = ChannelNameEnum.HH,
                ChannelId = ulong.Parse(Environment.GetEnvironmentVariable("HH_ID") ?? string.Empty),
                Categories = ["The Horus Heresy", "The Horus Heresy News"]
            },

            new ChannelConfig
            {
                ChannelName = ChannelNameEnum.BB,
                ChannelId = ulong.Parse(Environment.GetEnvironmentVariable("BB_ID") ?? string.Empty),
                Categories = ["Blood Bowl"]
            }
        ];
    }

    private static void InitializeRssFeedChecker()
    {
        _rssCheckTimer = new Timer(300000);
        _rssCheckTimer.Elapsed += async (sender, e) => await CheckRssFeedAsync();
        _rssCheckTimer.AutoReset = true; // Repeat the timer interval
        _rssCheckTimer.Enabled = true; // Start the timer

        _ = Task.Run(() => CheckRssFeedAsync());
    }

    private static async Task CheckRssFeedAsync()
    {
        Console.WriteLine("Checking Warhammer Community RSS feed...");
        using var reader = XmlReader.Create(_feedUrl);
        var feed = SyndicationFeed.Load(reader);

        var categoryTrackers = _dataService.GetCategoryTrackers();

        foreach (var config in _channelConfigs)
        {
            var item = feed.Items
                .Where(item => config.Categories.Any(category => item.Categories
                    .Any(feedCategory => feedCategory.Name
                        .Equals(category, StringComparison.CurrentCultureIgnoreCase))))
                .MaxBy(item => item.PublishDate);


            if (item != null)
            {
                var itemDateTime = item.PublishDate.UtcDateTime;
                var categoryTracker = categoryTrackers.Find(ct => ct.ChannelName == config.ChannelName.ToString()).FirstOrDefault();

                if (categoryTracker == null || itemDateTime > categoryTracker.LastPostedItemTimeStamp)
                {
                    var channel = await _client.GetChannelAsync(config.ChannelId);

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
                    Console.WriteLine($"Posted new item to channel {channel.Name}: {item.Title.Text}");

                    _dataService.UpdateLastPostedItemTimestamp(config.ChannelName, item.PublishDate.UtcDateTime);
                }
            }
        }
        Console.WriteLine("RSS");
    }
}
