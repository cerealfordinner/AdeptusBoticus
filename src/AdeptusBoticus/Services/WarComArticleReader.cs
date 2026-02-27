using System.Net.Http.Json;
using AdeptusBoticus.Models;

namespace AdeptusBoticus.Services;

public class WarComArticleReader : IWarComArticleReader
{
    private readonly HttpClient _httpClient;

    public WarComArticleReader(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<WarComResponse?> ReadArticlesAsync(string url)
    {
        // The API requires a POST request, even if there's no body
        var response = await _httpClient.PostAsync(url, null);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<WarComResponse>();
    }
}
