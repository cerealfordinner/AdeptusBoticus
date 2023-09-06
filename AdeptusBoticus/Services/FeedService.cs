using System.Reflection.Metadata;
using System.Xml;
using System.Xml.Linq;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace AdeptusBoticus;

public class FeedService
{
    private readonly IConfiguration _configuration;
    private string _discordApiKey;
    private ulong _channelId;
    private string _feedUrl;
    public FeedService(IConfiguration configuration)
    {
        _configuration = configuration;
        // get all config values
        _feedUrl = _configuration["FeedUrl"];
        _discordApiKey = _configuration["DiscordApiKey"];
        var ulongString = _configuration["ChannelId"];
        if (ulong.TryParse(ulongString, out ulong channelId))
        {
            _channelId = channelId;
        }
    }

    public async Task RunAsync()
    {
        var client = new DiscordSocketClient();
        client.Log += LogAsync;
        client.Ready += async () =>
        {
            Console.WriteLine("Bot is ready!");
            await CheckForNewArticle(client);
            await Task.CompletedTask;
        };
        await client.LoginAsync(Discord.TokenType.Bot, _discordApiKey);
        await client.StartAsync();

        await Task.Delay(-1);
    }

    private  async Task CheckForNewArticle(DiscordSocketClient client)
    {
        var storageService = new StorageService();

        while (true)
        {
            // Get latest article that meets the category criteria
            var xmlDoc = await GetFeedAsync(_feedUrl);
            Article latestArticle = GetLatestWarhammerArticle(xmlDoc);

            // Get the dateTime of the last article posted
            DateTime latestPostedArticleDate = storageService.GetLastPostedArticleDate();
            if (latestArticle != null && latestArticle.PublicationDate > latestPostedArticleDate)
            {
                await PostLatestArticle(client, latestArticle.Link);
                storageService.UpdateLastPostedArticleDate(latestArticle.PublicationDate);
                Console.WriteLine(latestArticle.Title);
            }
            else
            {
                Console.WriteLine("No new article found");
            }

            int waitTimeInMinutes = 1;
            await Task.Delay(TimeSpan.FromMinutes(waitTimeInMinutes));
        }
    }

    private static async Task LogAsync(LogMessage message)
    {
        Console.WriteLine($"Log message: {message}");
    }

    private async Task PostLatestArticle(DiscordSocketClient client, string articleLink)
    {
        var channel = client.GetChannel(_channelId) as ISocketMessageChannel;
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

    public Article GetLatestWarhammerArticle(XDocument xmlDoc)
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
