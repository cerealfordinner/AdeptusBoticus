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
        if (DateTime.TryParseExact(Date, "dd MMM yy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt))
        {
            var year = dt.Year;
            if (year < 100)
            {
                year += year < 50 ? 2000 : 1900;
            }
            return new DateTime(year, dt.Month, dt.Day, 12, 0, 0, DateTimeKind.Utc);
        }
        if (DateTime.TryParse(Date, out var dt2))
        {
            return dt2;
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
