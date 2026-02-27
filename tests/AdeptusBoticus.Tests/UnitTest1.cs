using AdeptusBoticus.Extensions;
using AdeptusBoticus.Models;
using AdeptusBoticus.Data;
using Microsoft.Extensions.Logging;
using Moq;
using MongoDB.Driver;

namespace AdeptusBoticus.Tests;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("<p>Hello World</p>", "Hello World")]
    [InlineData("<h1>Title</h1>", "Title")]
    [InlineData("No tags here", "No tags here")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("<div><p>Nested</p></div>", "Nested")]
    [InlineData("Text with &lt;escaped&gt; tags", "Text with escaped tags")]
    public void StripHtmlTags_RemovesHtmlTags(string? input, string expected)
    {
        var result = input.StripHtmlTags();
        
        Assert.Equal(expected, result);
    }
}

public class ChannelConfigTests
{
    [Fact]
    public void ChannelConfig_SetsPropertiesCorrectly()
    {
        var config = new ChannelConfig
        {
            ChannelName = ChannelNameEnum.WH40K,
            ChannelId = 123456789UL,
            Categories = ["Warhammer 40000", "40k"]
        };

        Assert.Equal(ChannelNameEnum.WH40K, config.ChannelName);
        Assert.Equal(123456789UL, config.ChannelId);
        Assert.Equal(2, config.Categories.Count);
        Assert.Contains("Warhammer 40000", config.Categories);
    }

    [Fact]
    public void ChannelConfig_RequiresCategories()
    {
        var config = new ChannelConfig
        {
            ChannelName = ChannelNameEnum.AOS,
            ChannelId = 987654321UL,
            Categories = ["AoS"]
        };

        Assert.NotNull(config.Categories);
    }
}

public class CategoryTrackerTests
{
    [Fact]
    public void CategoryTracker_SetsPropertiesCorrectly()
    {
        var tracker = new CategoryTracker
        {
            Id = "507f1f77bcf86cd799439011",
            ChannelName = "WH40K",
            LastPostedItemTimeStamp = DateTime.UtcNow
        };

        Assert.Equal("507f1f77bcf86cd799439011", tracker.Id);
        Assert.Equal("WH40K", tracker.ChannelName);
    }
}

public class DataServiceTests
{
    private readonly Mock<ILogger<DataService>> _mockLogger;

    public DataServiceTests()
    {
        _mockLogger = new Mock<ILogger<DataService>>();
    }

    [Fact]
    public void DataService_Constructor_InitializesDatabase()
    {
        var mongoUri = "mongodb://localhost:27017";
        
        var dataService = new DataService(mongoUri, _mockLogger.Object);
        
        Assert.NotNull(dataService);
    }
}
