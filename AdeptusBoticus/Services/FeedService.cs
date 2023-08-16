using System.Xml;
using System.Xml.Linq;

namespace AdeptusBoticus;

public class FeedService
{
    public static async Task<XDocument> GetFeedAsync(string feedUrl)
    {
        using var httpClient = new HttpClient();
        string xmlContent = await httpClient.GetStringAsync(feedUrl);
        return XDocument.Load(xmlContent);
    }

}
