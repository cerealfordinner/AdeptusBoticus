using AdeptusBoticus.Data;
using AdeptusBoticus.Models;
using AdeptusBoticus.Services;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Text.Json;

namespace AdeptusBoticus.Tests;

public class WarComArticleServiceTests
{
    private readonly BotConfiguration _config;
    private readonly IDataService _dataService;
    private readonly IDiscordBot _discordBot;
    private readonly IWarComArticleReader _articleReader;
    private readonly ILogger<WarComArticleService> _logger;
    private readonly WarComArticleService _service;

    public WarComArticleServiceTests()
    {
        _config = new BotConfiguration
        {
            FeedUrl = "http://test-api.com",
            Channels = new List<ChannelConfig>
            {
                new()
                {
                    ChannelName = ChannelNameEnum.WH40K,
                    ChannelId = 12345,
                    Categories = ["Warhammer 40,000"]
                }
            }
        };

        _dataService = Substitute.For<IDataService>();
        _discordBot = Substitute.For<IDiscordBot>();
        _articleReader = Substitute.For<IWarComArticleReader>();
        _logger = Substitute.For<ILogger<WarComArticleService>>();

        _service = new WarComArticleService(_config, _dataService, _discordBot, _articleReader, _logger);
    }

    [Fact]
    public async Task CheckArticlesAsync_PostsNewItem_WhenNewItemExists()
    {
        // Arrange
        var json = await File.ReadAllTextAsync("sample-response.json");
        var response = JsonSerializer.Deserialize<WarComResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        _articleReader.ReadArticlesAsync(_config.FeedUrl).Returns(response);

        // Tracker is null (or older than the articles)
        _dataService.GetTracker(ChannelNameEnum.WH40K).Returns((CategoryTracker?)null);

        // Mock Discord Channel
        _discordBot.GetChannelAsync(Arg.Any<ulong>()).Returns(Substitute.For<DiscordChannel>());

        // Act
        await _service.CheckArticlesAsync();

        // Assert
        // The newest 40k article in the sample is "Aeldari Corsair showcase: The Ballad of the Sky Serpents part two" or "First look at the datasheet for the new Tyranid Prime with Lash Whip"
        // Since there are multiple "27 Feb 26" articles, we just check we sent at least one message.
        await _discordBot.Received(1).SendMessageAsync(
            Arg.Is<ulong>(12345),
            Arg.Any<DiscordEmbed>()
        );

        _dataService.Received(1).UpdateLastPostedItemTimestamp(ChannelNameEnum.WH40K, Arg.Any<DateTime>());
    }

    [Fact]
    public async Task CheckArticlesAsync_DoesNotPost_WhenItemIsOlderThanTracker()
    {
        // Arrange
        var json = await File.ReadAllTextAsync("sample-response.json");
        var response = JsonSerializer.Deserialize<WarComResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        _articleReader.ReadArticlesAsync(_config.FeedUrl).Returns(response);

        // Tracker has a date from the distant future
        _dataService.GetTracker(ChannelNameEnum.WH40K).Returns(new CategoryTracker
        {
            ChannelName = "WH40K",
            LastPostedItemTimeStamp = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        // Act
        await _service.CheckArticlesAsync();

        // Assert
        await _discordBot.DidNotReceiveWithAnyArgs().SendMessageAsync(default, default!);
        _dataService.DidNotReceiveWithAnyArgs().UpdateLastPostedItemTimestamp(default, default);
    }
}