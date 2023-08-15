using PuppeteerSharp;

namespace AdeptusBoticus;

public class WebScrapingService
{
    public async Task<List<Article>> ScrapeArticlesAsync(string url)
    {
        var articles = new List<Article>();

        await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

        return articles;
    }
}