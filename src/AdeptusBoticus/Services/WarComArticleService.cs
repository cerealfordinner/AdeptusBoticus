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
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting RSS polling cycle at {StartTime}", startTime);

        try
        {
            _logger.LogDebug("Fetching articles from Warhammer Community API: {FeedUrl}", _config.FeedUrl);
            var response = await _articleReader.ReadArticlesAsync(_config.FeedUrl);

            if (response?.News == null || !response.News.Any())
            {
                _logger.LogWarning("No articles returned from API.");
                return;
            }

            _logger.LogInformation("API returned {ArticleCount} articles", response.News.Count);

            var postsMade = 0;
            var channelsChecked = 0;

            foreach (var channelConfig in _config.Channels)
            {
                channelsChecked++;
                _logger.LogDebug("Checking channel {ChannelName} (ID: {ChannelId}) for categories: {Categories}",
                    channelConfig.ChannelName, channelConfig.ChannelId,
                    string.Join(", ", channelConfig.Categories));

                var matchingItems = response.News
                    .Where(item => channelConfig.Categories.Any(category => item.Topics
                        .Any(feedTopic => feedTopic.Title
                            .Equals(category, StringComparison.OrdinalIgnoreCase))));

                var item = matchingItems.Any()
                    ? matchingItems.MaxBy(item => item.GetParsedDate())
                    : null;

                if (item != null)
                {
                    var itemDateTime = item.GetParsedDate().ToUniversalTime();
                    var categoryTracker = _dataService.GetTracker(channelConfig.ChannelName);
                    var fullUrl = $"https://www.warhammer-community.com{item.Uri}";

                    _logger.LogInformation("Newest matching article for {ChannelName}: {ItemTitle} (Date: {ItemDate}, URL: {Url})",
                        channelConfig.ChannelName, item.Title, itemDateTime, fullUrl);

                    if (categoryTracker == null || itemDateTime > categoryTracker.LastPostedItemTimeStamp)
                    {
                        _logger.LogInformation("Article is newer than last posted (Last: {LastPosted}, New: {ArticleDate}) - posting to Discord",
                            categoryTracker?.LastPostedItemTimeStamp, itemDateTime);

                        string? thumbnailUrl = null;
                        if (!string.IsNullOrEmpty(item.Image?.Path))
                        {
                            thumbnailUrl = $"https://assets.warhammer-community.com/{item.Image.Path}";
                        }

                        var embed = new DiscordEmbedBuilder
                        {
                            Title = item.Title.StripHtmlTags(),
                            Url = fullUrl,
                            Description = item.Excerpt.StripHtmlTags(),
                            ImageUrl = thumbnailUrl
                        };

                        try
                        {
                            await _discordBot.SendMessageAsync(channelConfig.ChannelId, embed);
                            postsMade++;
                            _logger.LogInformation("Successfully posted to Discord channel {ChannelName} (ID: {ChannelId})",
                                channelConfig.ChannelName, channelConfig.ChannelId);

                            _dataService.UpdateLastPostedItemTimestamp(channelConfig.ChannelName, itemDateTime);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to post article to Discord channel {ChannelName}", channelConfig.ChannelName);
                            throw;
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Article is not newer than last posted for {ChannelName}. Last posted: {LastPosted}, Article date: {ArticleDate} - skipping",
                            channelConfig.ChannelName, categoryTracker?.LastPostedItemTimeStamp, itemDateTime);
                    }
                }
                else
                {
                    _logger.LogDebug("No matching articles found for {ChannelName}", channelConfig.ChannelName);
                }
            }

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("RSS polling cycle completed. Checked {ChannelCount} channels, made {PostsMade} posts. Duration: {Duration}ms",
                channelsChecked, postsMade, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "RSS polling cycle failed after {Duration}ms", duration.TotalMilliseconds);
            throw;
        }
    }
}
