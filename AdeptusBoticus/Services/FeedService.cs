using System.Xml.Linq;
using Discord;
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
    private ulong _warcomChannelId;
    private ulong _ordersChannelId;
    private string _feedUrl;
    public FeedService(IConfiguration configuration)
    {
        _configuration = configuration;
        GetConfigs(_configuration);
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
        Article new40KArticle = GetNewWarhammerArticle(xmlDoc, _40kCategories, ArticleType.Warhammer40K, latest40kArticleDate);
        Article newHorusHersyArticle = GetNewWarhammerArticle(xmlDoc, _horusHeresyCategories, ArticleType.WarhammerHorusHeresy, latestHorusHeresyArticleDate);
        Article newFantasyArticle = GetNewWarhammerArticle(xmlDoc, _fantasyCategories, ArticleType.WarhammerFantasy, latestFantasyArticleDate);

        if (new40KArticle != null || newHorusHersyArticle != null || newFantasyArticle != null)
        {
            if (new40KArticle != null && new40KArticle.PublicationDate > latest40kArticleDate)
            {
                await PostLatestArticle(_client, new40KArticle);
                storageService.UpdateLastPostedArticleDate(new40KArticle);
                Console.WriteLine(new40KArticle.Title);
            }
            if (newHorusHersyArticle != null && newHorusHersyArticle.PublicationDate > latestHorusHeresyArticleDate)
            {
                await PostLatestArticle(_client, newHorusHersyArticle);
                storageService.UpdateLastPostedArticleDate(newHorusHersyArticle);
                Console.WriteLine(newHorusHersyArticle.Title);
            }
            if (newFantasyArticle != null && newFantasyArticle.PublicationDate > latestFantasyArticleDate)
            {
                await PostLatestArticle(_client, newFantasyArticle);
                storageService.UpdateLastPostedArticleDate(newFantasyArticle);
                Console.WriteLine(newFantasyArticle.Title);
            }
        }
        else
        {
            Console.WriteLine("No new articles found");
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

        if (article.Title.Contains("pre-order", StringComparison.CurrentCultureIgnoreCase))
        {
            var orderChannel = client.GetChannel(_ordersChannelId) as ISocketMessageChannel;
            await orderChannel?.SendMessageAsync(embed: new EmbedBuilder
            {
                Title = article.Title,
                Description = article.Description.StripHtmlTags(),
                ImageUrl = article.ImageUrl,
                Url = article.Link
            }.Build());
        }
        else
        {
            var gameSystemChannel = client.GetChannel(channelId) as ISocketMessageChannel;
            await gameSystemChannel?.SendMessageAsync(embed: new EmbedBuilder
            {
                Title = article.Title,
                Description = article.Description.StripHtmlTags(),
                ImageUrl = article.ImageUrl,
                Url = article.Link
            }.Build());
        }
        var warcomChannel = client.GetChannel(_warcomChannelId) as ISocketMessageChannel;
        await warcomChannel?.SendMessageAsync(embed: new EmbedBuilder
        {
            Title = article.Title,
            Description = article.Description.StripHtmlTags(),
            ImageUrl = article.ImageUrl,
            Url = article.Link
        }.Build());

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
                    ImageUrl = latestArticleElement.Element("image")?.Value,
                    Description = latestArticleElement.Element("description")?.Value,
                    ArticleType = articleType,
                    PublicationDate = articleDate
                };
            }
        }

        return null;
    }

    private void GetConfigs(IConfiguration configuration)
    {
        _feedUrl = _configuration["FeedUrl"];
        _discordApiKey = _configuration["DiscordApiKey"];

        _40kChannelId = ulong.TryParse(configuration["Warhammer40K:channelId"], out ulong channelId40k) ? channelId40k : 0;
        _40kCategories = configuration.GetSection("Warhammer40K:categories").GetChildren().Select(x => x.Value).ToArray();

        _horusHersyChannelId = ulong.TryParse(configuration["WarhammerHorusHeresy:channelId"], out ulong channelidHorusHeresy) ? channelidHorusHeresy : 0;
        _horusHeresyCategories = configuration.GetSection("WarhammerHorusHeresy:categories").GetChildren().Select(x => x.Value).ToArray();

        _fantasyChannelId = ulong.TryParse(configuration["WarhammerFantasy:channelId"], out ulong channelIdFantasy) ? channelIdFantasy : 0;
        _fantasyCategories = configuration.GetSection("WarhammerFantasy:categories").GetChildren().Select(x => x.Value).ToArray();

        _warcomChannelId = ulong.TryParse(configuration["Warcom:channelId"], out ulong channelIdWarcom) ? channelIdWarcom : 0;
        _ordersChannelId = ulong.TryParse(configuration["Orders:channelId"], out ulong channelIdOrder) ? channelIdOrder : 0;
    }
}

