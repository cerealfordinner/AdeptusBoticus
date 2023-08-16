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
        return XDocument.Load(xmlContent);
    }

    public Article GetLatestArticle(XDocument xmlDoc)
    {
        var articles = xmlDoc.Descendants("item");
        var latestArticleElement = articles.FirstOrDefault();

        if (latestArticleElement != null)
        {
            Article latestArticle = new Article
            {
                Title = latestArticleElement.Element("title")?.Value,
                Link = latestArticleElement.Element("link")?.Value,
            };
        }
    }

}
