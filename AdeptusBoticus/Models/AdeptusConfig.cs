namespace AdeptusBoticus.Models;

public class AdeptusConfig
{
    public string? FeedUrl { get; set; }
    public string? DiscordApiKey { get; set; }
    public ulong ChannelId40k { get; set; }
    public ulong ChannelIdFantasy { get; set; }
    public ulong ChannelIdHorusHeresy { get; set; }
    public ulong ChannelIdWarcom { get; set; }
    public ulong ChannelIdOrders { get; set; }
}