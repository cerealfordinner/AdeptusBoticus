using System.Xml.Linq;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace AdeptusBoticus;

public class FeedService
{
    private readonly IConfiguration _configuration;
    private DiscordSocketClient _client;
    private static System.Timers.Timer _articleCheckTimer;
    private string _discordApiKey;
    private ulong _40kChannelId;
    private string[] _40kCategories;
    private ulong _horusHersyChannelId;
    private string[] _horusHeresyCategories;
    private ulong _fantasyChannelId;
    private string[] _fantasyCategories;
    private string _feedUrl;
    public FeedService(IConfiguration configuration)
    {
        _configuration = configuration;
        // get all config values
        _feedUrl = _configuration["FeedUrl"];
        _discordApiKey = _configuration["DiscordApiKey"];

        var ulong40k = configuration.GetSection("Warhammer40K:channelId").Value;
        if (ulong.TryParse(ulong40k, out ulong channelId40k))
        {
            _40kChannelId = channelId40k;
        }
        _40kCategories = configuration.GetSection("Warhammer40K:categories")
            .GetChildren()
            .Select(x => x.Value)
            .ToArray();

        var ulongHH = configuration.GetSection("WarhammerHorusHeresy:channelId").Value;
        if (ulong.TryParse(ulongHH, out ulong channelIdHH))
        {
            _horusHersyChannelId = channelIdHH;
        }
        _horusHeresyCategories = configuration.GetSection("WarhammerHorusHeresy:categories")
            .GetChildren()
            .Select(x => x.Value)
            .ToArray();

        var ulongFantasy = configuration.GetSection("WarhammerFantasy:channelId").Value;
        if (ulong.TryParse(ulongFantasy, out ulong channelIdFantasy))
        {
            _fantasyChannelId = channelIdFantasy;
        }
        _fantasyCategories = configuration.GetSection("WarhammerFantasy:categories")
            .GetChildren()
            .Select(x => x.Value)
            .ToArray();
    }

    public async Task RunBotAsync()
    {
        _client = new DiscordSocketClient();
        _client.Log += LogAsync;

        _client.Ready += ReadyAsync;

        await _client.LoginAsync(Discord.TokenType.Bot, _discordApiKey);
        await _client.StartAsync();

        await Task.Delay(-1);
    }

    private async Task ReadyAsync()
    {
        StartArticleCheckTimer();
        await CheckForNewArticle();
    }

    private void StartArticleCheckTimer()
    {
        _articleCheckTimer = new System.Timers.Timer
        {
            Interval = TimeSpan.FromMinutes(5).TotalMilliseconds,
            AutoReset = true,
            Enabled = true
        };

        _articleCheckTimer.Elapsed += async (sender, e) => await CheckForNewArticle();

        _articleCheckTimer.Start();
    }

    public async Task CheckForNewArticle()
    {
        var storageService = new StorageService();

        DateTime latest40kArticleDate = storageService.GetLastPostedArticleDate(ArticleType.Warhammer40K);
        DateTime latestHorusHeresyArticleDate = storageService.GetLastPostedArticleDate(ArticleType.WarhammerHorusHeresy);
        DateTime latestFantasyArticleDate = storageService.GetLastPostedArticleDate(ArticleType.WarhammerFantasy);

        var xmlDoc = await GetFeedAsync(_feedUrl);
        Article latest40KArticle = GetNewWarhammerArticle(xmlDoc, _40kCategories, ArticleType.Warhammer40K, latest40kArticleDate);
        Article latestHorusHersyArticle = GetNewWarhammerArticle(xmlDoc, _horusHeresyCategories, ArticleType.WarhammerHorusHeresy, latestHorusHeresyArticleDate);
        Article latestFantasyArticle = GetNewWarhammerArticle(xmlDoc, _fantasyCategories, ArticleType.WarhammerFantasy, latestFantasyArticleDate);

        if (latest40KArticle != null || latestHorusHersyArticle != null || latestFantasyArticle != null)
        {
            if (latest40KArticle != null && latest40KArticle.PublicationDate > latest40kArticleDate)
            {
                await PostLatestArticle(_client, latest40KArticle);
                storageService.UpdateLastPostedArticleDate(latest40KArticle);
                Console.WriteLine(latest40KArticle.Title);
            }
            if (latestHorusHersyArticle != null && latestHorusHersyArticle.PublicationDate > latestHorusHeresyArticleDate)
            {
                await PostLatestArticle(_client, latestHorusHersyArticle);
                storageService.UpdateLastPostedArticleDate(latestHorusHersyArticle);
                Console.WriteLine(latestHorusHersyArticle.Title);
            }
            if (latestFantasyArticle != null && latestFantasyArticle.PublicationDate > latestFantasyArticleDate)
            {
                await PostLatestArticle(_client, latestFantasyArticle);
                storageService.UpdateLastPostedArticleDate(latestFantasyArticle);
                Console.WriteLine(latestFantasyArticle.Title);
            }
        }

    }

    private static async Task LogAsync(LogMessage message)
    {
        Console.WriteLine($"Log message: {message}");
    }

    private async Task PostLatestArticle(DiscordSocketClient client, Article article)
    {
        ulong channelId = new ulong();
        switch (article.ArticleType)
        {
            case ArticleType.Warhammer40K:
                channelId = _40kChannelId;
                break;
            case ArticleType.WarhammerHorusHeresy:
                channelId = _horusHersyChannelId;
                break;
            case ArticleType.WarhammerFantasy:
                channelId = _fantasyChannelId;
                break;
        }

        var channel = client.GetChannel(channelId) as ISocketMessageChannel;

        if (channel != null)
        {
            await channel.SendMessageAsync(article.Link);
        }
    }

    public async Task<XDocument> GetFeedAsync(string feedUrl)
    {
        using var httpClient = new HttpClient();
        string xmlContent = await httpClient.GetStringAsync(feedUrl);
        XDocument xmlDoc = XDocument.Parse(xmlContent);
        return xmlDoc;
    }

    public Article GetNewWarhammerArticle(XDocument xmlDoc, string[] categories, ArticleType articleType, DateTime lastArticleDate)
    {
        var latestArticleElement = xmlDoc.Descendants("item")
        .Where(item => item.Elements("category").Any(categoryElement => categories.Contains(categoryElement.Value)))
        .OrderByDescending(x => DateTimeHelper.GetPublicationDateFromElement(x))
        .FirstOrDefault();

        if (latestArticleElement != null)
        {
            string articleDateString = latestArticleElement.Element("pubDate")?.Value;
            DateTime.TryParse(articleDateString, out DateTime articleDate);

            if (articleDate > lastArticleDate)
            {
                return new Article
                {
                    Title = latestArticleElement.Element("title")?.Value,
                    Link = latestArticleElement.Element("link")?.Value,
                    ArticleType = articleType,
                    PublicationDate = articleDate
                };
            }
        }

        return null;
    }

}

