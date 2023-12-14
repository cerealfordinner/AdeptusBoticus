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
    private CommandService _commandService;
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
        _horusHeresyCategories = configuration.GetSection("Warhammer40K:categories")
            .GetChildren()
            .Select(x => x.Value)
            .ToArray();

        var ulongFantasy = configuration.GetSection("WarhammerFantasy:channelId").Value;
        if (ulong.TryParse(ulongFantasy, out ulong channelIdFantasy))
        {
            _fantasyChannelId = channelIdFantasy;
        }
        _fantasyCategories = configuration.GetSection("Warhammer40K:categories")
            .GetChildren()
            .Select(x => x.Value)
            .ToArray();
    }

    public async Task RunBotAsync()
    {
        _client = new DiscordSocketClient();
        _commandService = new CommandService();
        _client.Log += LogAsync;
        await _client.LoginAsync(Discord.TokenType.Bot, _discordApiKey);
        await _client.StartAsync();

        // StartArticleCheckTimer();
        await CheckForNewArticle();

        await Task.Delay(-1);
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

        // Get latest article that meets the category criteria
        var xmlDoc = await GetFeedAsync(_feedUrl);
        Article latest40KArticle = GetLatestWarhammerArticle(xmlDoc, _40kCategories);
        Article latestHorusHersyArticle = GetLatestWarhammerArticle(xmlDoc, _horusHeresyCategories);
        Article latestFantasyArticle = GetLatestWarhammerArticle(xmlDoc, _fantasyCategories);

        // Get the dateTime of the last article posted
        DateTime latestPostedArticleDate = storageService.GetLastPostedArticleDate();
        if (latest40KArticle != null && latest40KArticle.PublicationDate > latestPostedArticleDate)
        {
            await PostLatestArticle(_client, latest40KArticle.Link);
            storageService.UpdateLastPostedArticleDate(latest40KArticle.PublicationDate);
            Console.WriteLine(latest40KArticle.Title);
        }
        else
        {
            Console.WriteLine("No new article found");
        }
    }

    private static async Task LogAsync(LogMessage message)
    {
        Console.WriteLine($"Log message: {message}");
    }

    private async Task PostLatestArticle(DiscordSocketClient client, string articleLink)
    {
        var channel = client.GetChannel(_40kChannelId) as ISocketMessageChannel;
        if (channel != null)
        {
            await channel.SendMessageAsync(articleLink);
        }
    }

    public async Task<XDocument> GetFeedAsync(string feedUrl)
    {
        using var httpClient = new HttpClient();
        string xmlContent = await httpClient.GetStringAsync(feedUrl);
        XDocument xmlDoc = XDocument.Parse(xmlContent);
        return xmlDoc;
    }

    public Article GetLatestWarhammerArticle(XDocument xmlDoc, string[] categories)
    {
        var articles = xmlDoc.Descendants("item");
        var latestArticleElement = articles.Where(x => x.Elements("category")
        .Any(x => x.Value.Contains("40k") || x.Value.Contains("Warhammer 40k")))
        .OrderByDescending(x => DateTimeHelper.GetPublicationDateFromElement(x))
        .FirstOrDefault();

        if (latestArticleElement != null)
        {
            string dateString = latestArticleElement.Element("pubDate")?.Value;
            
            Article latestArticle = new Article
            {
                Title = latestArticleElement.Element("title")?.Value,
                Link = latestArticleElement.Element("link")?.Value,
           };

            if(DateTime.TryParse(dateString, out DateTime date))
            {
                latestArticle.PublicationDate = date;
            }

            return latestArticle;
        }

        return null;
    }

}
