using System.Xml;
using System.Xml.Linq;

namespace AdeptusBoticus;

public class FeedService
{
    public string FeedUrl { get; set; }
    public FeedService(string feedUrl)
    {
        FeedUrl = feedUrl;
    }

    public static async Task RunAsync()
    {
        var storageService = new StorageService();

        while (true)
        {
            // Get latest article that meets the category criteria
            var feedService = new FeedService("https://www.warhammer-community.com/feed/");
            var xmlDoc = await feedService.GetFeedAsync();
            Article latestArticle = feedService.GetLatestWarhammerArticle(xmlDoc);

            // Get the dateTime of the last article posted
            DateTime latestPostedArticleDate = storageService.GetLastPostedArticleDate();
            if (latestArticle != null && latestArticle.PublicationDate > latestPostedArticleDate)
            {
                storageService.UpdateLastPostedArticleDate(latestArticle.PublicationDate);
                Console.WriteLine(latestArticle.Id);
            }
            else
            {
                Console.WriteLine("No new article found");
            }

            int waitTimeInMinutes = 1;
            await Task.Delay(TimeSpan.FromMinutes(waitTimeInMinutes));
        }
    }

    public async Task<XDocument> GetFeedAsync()
    {
        using var httpClient = new HttpClient();
        string xmlContent = await httpClient.GetStringAsync(FeedUrl);
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
                Id = latestArticleElement.Element("guid").Value.GetArticleIdFromlink(),
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
