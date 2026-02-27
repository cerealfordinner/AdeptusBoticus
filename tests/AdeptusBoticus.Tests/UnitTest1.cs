using AdeptusBoticus.Extensions;
using AdeptusBoticus.Models;
using AdeptusBoticus.Data;
using Microsoft.Extensions.Logging;
using Moq;

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
            ChannelName = "WH40K",
            LastPostedItemTimeStamp = DateTime.UtcNow
        };

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
    public void DataService_Constructor_InitializesWithEmptyState()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"adeptusboticus_test_{Guid.NewGuid()}.json");

        try
        {
            var dataService = new DataService(tempFile, _mockLogger.Object);

            Assert.NotNull(dataService);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void DataService_InitializeCategoryTimestamps_CreatesAllTrackers()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"adeptusboticus_test_{Guid.NewGuid()}.json");

        try
        {
            var dataService = new DataService(tempFile, _mockLogger.Object);
            dataService.InitializeCategoryTimestamps();

            foreach (ChannelNameEnum channel in Enum.GetValues(typeof(ChannelNameEnum)))
            {
                var tracker = dataService.GetTracker(channel);
                Assert.NotNull(tracker);
                Assert.Equal(channel.ToString(), tracker.ChannelName);
            }
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void DataService_UpdateLastPostedItemTimestamp_UpdatesExistingTracker()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"adeptusboticus_test_{Guid.NewGuid()}.json");

        try
        {
            var dataService = new DataService(tempFile, _mockLogger.Object);
            dataService.InitializeCategoryTimestamps();

            var newTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
            dataService.UpdateLastPostedItemTimestamp(ChannelNameEnum.WH40K, newTime);

            var tracker = dataService.GetTracker(ChannelNameEnum.WH40K);
            Assert.NotNull(tracker);
            Assert.Equal(newTime, tracker.LastPostedItemTimeStamp);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void DataService_PersistsDataToDisk()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"adeptusboticus_test_{Guid.NewGuid()}.json");

        try
        {
            var newTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

            // Write data with one instance
            var dataService1 = new DataService(tempFile, _mockLogger.Object);
            dataService1.InitializeCategoryTimestamps();
            dataService1.UpdateLastPostedItemTimestamp(ChannelNameEnum.AOS, newTime);

            // Read it back with a new instance
            var dataService2 = new DataService(tempFile, _mockLogger.Object);
            var tracker = dataService2.GetTracker(ChannelNameEnum.AOS);

            Assert.NotNull(tracker);
            Assert.Equal(newTime, tracker.LastPostedItemTimeStamp);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
