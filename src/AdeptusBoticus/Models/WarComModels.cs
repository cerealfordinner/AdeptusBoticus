using System.Text.Json.Serialization;

namespace AdeptusBoticus.Models;

public class WarComResponse
{
    [JsonPropertyName("news")]
    public List<WarComArticle> News { get; set; } = new();
}

public class WarComArticle
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("excerpt")]
    public string Excerpt { get; set; } = string.Empty;

    [JsonPropertyName("image")]
    public WarComImage? Image { get; set; }

    [JsonPropertyName("topics")]
    public List<WarComTopic> Topics { get; set; } = new();

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    public DateTime GetParsedDate()
    {
        if (DateTime.TryParse(Date, out var dt))
        {
            return dt;
        }
        return DateTime.MinValue;
    }
}

public class WarComImage
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;
}

public class WarComTopic
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
}
