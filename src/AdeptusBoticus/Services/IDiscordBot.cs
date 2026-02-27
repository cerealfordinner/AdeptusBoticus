using DSharpPlus.Entities;

namespace AdeptusBoticus.Services;

public interface IDiscordBot
{
    Task ConnectAsync();
    Task<DiscordChannel> GetChannelAsync(ulong channelId);
    Task SendMessageAsync(ulong channelId, DiscordEmbed embed);
}
