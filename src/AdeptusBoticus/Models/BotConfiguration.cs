namespace AdeptusBoticus.Models;

public class BotConfiguration
{
    public string DiscordToken { get; set; } = string.Empty;
    public string MongoDbUri { get; set; } = "mongodb://mongodb:27017";
    public string FeedUrl { get; set; } = "https://www.warhammer-community.com/feed/";
    public int RssCheckIntervalMs { get; set; } = 300000;
    public List<ChannelConfig> Channels { get; set; } = [];
}
