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
