using AdeptusBoticus.Models;
using Microsoft.Extensions.Configuration;

namespace AdeptusBoticus.Services;

public static class ConfigService
{
    public static AdeptusConfig GetConfigs(IConfiguration configuration)
    {
        var config = new AdeptusConfig();
        config.FeedUrl = configuration["FeedUrl"];
        config.DiscordApiKey = configuration["DiscordApiKey"];
        config.ChannelId40k = ulong.TryParse(configuration["Warhammer40K:channelId"], out ulong channelId40k) ? channelId40k : 0;
        config.ChannelIdHorusHeresy = ulong.TryParse(configuration["WarhammerHorusHeresy:channelId"], out ulong channelidHorusHeresy) ? channelidHorusHeresy : 0;
        config.ChannelIdFantasy = ulong.TryParse(configuration["WarhammerFantasy:channelId"], out ulong channelIdFantasy) ? channelIdFantasy : 0;
        config.ChannelIdWarcom = ulong.TryParse(configuration["Warcom:channelId"], out ulong channelIdWarcom) ? channelIdWarcom : 0;
        config.ChannelIdOrders = ulong.TryParse(configuration["Orders:channelId"], out ulong channelIdOrder) ? channelIdOrder : 0;
        return config;
    }
}