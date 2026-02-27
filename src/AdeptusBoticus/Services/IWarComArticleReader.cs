using AdeptusBoticus.Models;

namespace AdeptusBoticus.Services;

public interface IWarComArticleReader
{
    Task<WarComResponse?> ReadArticlesAsync(string url);
}
