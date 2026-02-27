using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AdeptusBoticus.Services;

public sealed class DiscordBot : IDiscordBot
{
    private readonly DiscordClient _client;
    private readonly ILogger<DiscordBot> _logger;
    
    public DiscordBot(string botToken, ILogger<DiscordBot> logger)
    {
        _logger = logger;
        var config = new DiscordConfiguration
        {
            Token = botToken,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged
        };
        
        _client = new DiscordClient(config);
    }

    public async Task ConnectAsync()
    {
        try
        {
            await _client.ConnectAsync();
            _logger.LogInformation("Bot connected to Discord!");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to connect to Discord");
            throw;
        }
    }

    public async Task<DiscordChannel> GetChannelAsync(ulong channelId)
    {
        return await _client.GetChannelAsync(channelId);
    }

    public async Task SendMessageAsync(ulong channelId, DiscordEmbed embed)
    {
        var channel = await _client.GetChannelAsync(channelId);
        await channel.SendMessageAsync(embed);
    }
}
