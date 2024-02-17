using System.Xml.Linq;
using AdeptusBoticus.Data;
using AdeptusBoticus.Helpers;
using AdeptusBoticus.Models;

namespace AdeptusBoticus.Services;

public static class ArticleFactory
{
    public static T GenerateArticle<T>(XElement? element) where T : IArticle, new()
    {
        string? articleDateString = element?.Element("pubDate")?.Value;
        DateTime.TryParse(articleDateString, out DateTime articleDate);

        return new T
        {
            Title = element.Element("title")?.Value,
            Link = element.Element("link")?.Value,
            ImageUrl = element.Element("image")?.Value,
            Description = element.Element("description")?.Value,
            PublicationDate = articleDate
        };
    }
    
    public static List<IArticle> GetNewWarhammerArticles(XDocument xmlDoc)
    {
        List<IArticle> articles = new List<IArticle>();
        
        var latest40kArticleElement = xmlDoc.Descendants("item")
            .Where(item =>
                item.Elements("category")
                    .Any(categoryElement => Categories.Warhammer40k.Contains(categoryElement.Value)))
            .MaxBy(DateTimeHelper.GetPublicationDateFromElement);
        if (latest40kArticleElement != null)
        {
            var latest40kArticle = GenerateArticle<Warhammer40KArticle>(latest40kArticleElement);
            articles.Add(latest40kArticle);
        }

        var latestHorusHeresyArticleElement = xmlDoc.Descendants("item")
            .Where(item =>
                item.Elements("category")
                    .Any(categoryElement => Categories.HorusHeresy.Contains(categoryElement.Value)))
            .MaxBy(DateTimeHelper.GetPublicationDateFromElement);
        if (latestHorusHeresyArticleElement != null)
        {
            var latestHorusHeresyArticle = GenerateArticle<Warhammer40KArticle>(latest40kArticleElement);
            articles.Add(latestHorusHeresyArticle);
        }

        var latestFantasyArticleElement = xmlDoc.Descendants("item")
            .Where(item =>
                item.Elements("category").Any(categoryElement => Categories.Fantasy.Contains(categoryElement.Value)))
            .MaxBy(DateTimeHelper.GetPublicationDateFromElement);
        if (latestFantasyArticleElement != null)
        {
            var latestFantasyArticle = GenerateArticle<Warhammer40KArticle>(latest40kArticleElement);
            articles.Add(latestFantasyArticle);
        }

        return articles;
    }
}