using AdeptusBoticus.Models;
using DSharpPlus.Entities;

namespace AdeptusBoticus.Services;

public interface IDiscordBot
{
    Task ConnectAsync();
    Task SendMessageAsync(ulong channelId, DiscordEmbed embed);
}
