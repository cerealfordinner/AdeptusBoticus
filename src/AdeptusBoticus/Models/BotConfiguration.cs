namespace AdeptusBoticus.Models;

public class BotConfiguration
{
    public string DiscordToken { get; set; } = string.Empty;
    public string DataFilePath { get; set; } = "./data/trackers.json";
    public string FeedUrl { get; set; } = "https://www.warhammer-community.com/api/search/news/";
    public int RssCheckIntervalMs { get; set; } = 300000;
    public List<ChannelConfig> Channels { get; set; } = [];
}
