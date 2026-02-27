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
        var payload = new
        {
            sortBy = "date_desc",
            category = "",
            collections = new[] { "articles" },
            game_systems = Array.Empty<string>(),
            index = "news",
            locale = "en-gb",
            page = 0,
            perPage = 16,
            topics = Array.Empty<string>()
        };

        var jsonContent = System.Text.Json.JsonSerializer.Serialize(payload);
        var response = await _httpClient.PostAsync(url, new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<WarComResponse>();
    }
}
