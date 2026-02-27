using System;
using System.Linq;
using System.Threading.Tasks;
using AdeptusBoticus.Data;
using AdeptusBoticus.Extensions;
using AdeptusBoticus.Models;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AdeptusBoticus.Services;

public class WarComArticleService : IWarComArticleService
{
    private readonly BotConfiguration _config;
    private readonly IDataService _dataService;
    private readonly IDiscordBot _discordBot;
    private readonly IWarComArticleReader _articleReader;
    private readonly ILogger<WarComArticleService> _logger;

    public WarComArticleService(
        BotConfiguration config,
        IDataService dataService,
        IDiscordBot discordBot,
        IWarComArticleReader articleReader,
        ILogger<WarComArticleService> logger)
    {
        _config = config;
        _dataService = dataService;
        _discordBot = discordBot;
        _articleReader = articleReader;
        _logger = logger;
    }

    public async Task CheckArticlesAsync()
    {
        _logger.LogDebug("Checking Warhammer Community API...");

        try
        {
            var response = await _articleReader.ReadArticlesAsync(_config.FeedUrl);
            
            if (response?.News == null || !response.News.Any())
            {
                _logger.LogWarning("No articles returned from API.");
                return;
            }

            foreach (var channelConfig in _config.Channels)
            {
                var item = response.News
                    .Where(item => channelConfig.Categories.Any(category => item.Topics
                        .Any(feedTopic => feedTopic.Title
                            .Equals(category, StringComparison.CurrentCultureIgnoreCase))))
                    .MaxBy(item => item.GetParsedDate());

                if (item != null)
                {
                    var itemDateTime = item.GetParsedDate().ToUniversalTime();
                    var categoryTracker = _dataService.GetTracker(channelConfig.ChannelName);

                    if (categoryTracker == null || itemDateTime > categoryTracker.LastPostedItemTimeStamp)
                    {
                        var channel = await _discordBot.GetChannelAsync(channelConfig.ChannelId);

                        string? thumbnailUrl = null;
                        if (!string.IsNullOrEmpty(item.Image?.Path))
                        {
                            // The path is usually relative to a media domain, but we'll try to use it as is
                            // Note: We might need to prepend a base URL if it's strictly a relative path
                            thumbnailUrl = $"https://assets.warhammer-community.com/{item.Image.Path}";
                        }

                        var fullUrl = $"https://www.warhammer-community.com{item.Uri}";

                        var embed = new DiscordEmbedBuilder
                        {
                            Title = item.Title.StripHtmlTags(),
                            Url = fullUrl,
                            Description = item.Excerpt.StripHtmlTags(),
                            ImageUrl = thumbnailUrl
                        };

                        await _discordBot.SendMessageAsync(channelConfig.ChannelId, embed);
                        _logger.LogInformation("Posted new item to channel {ChannelName}: {ItemTitle}", channel.Name, item.Title);

                        _dataService.UpdateLastPostedItemTimestamp(channelConfig.ChannelName, itemDateTime);
                    }
                }
            }
            _logger.LogDebug("API check complete.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check Warhammer Community API");
            throw;
        }
    }
}
