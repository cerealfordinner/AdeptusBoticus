using AdeptusBoticus.Data;
using AdeptusBoticus.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AdeptusBoticus.Tests;

public class DataServiceTests
{
    private readonly ILogger<DataService> _mockLogger;

    public DataServiceTests()
    {
        _mockLogger = Substitute.For<ILogger<DataService>>();
    }

    [Fact]
    public void DataService_Constructor_InitializesWithEmptyState()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"adeptusboticus_test_{Guid.NewGuid()}.json");

        try
        {
            var dataService = new DataService(tempFile, _mockLogger);

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
            var dataService = new DataService(tempFile, _mockLogger);
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
            var dataService = new DataService(tempFile, _mockLogger);
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
            var dataService1 = new DataService(tempFile, _mockLogger);
            dataService1.InitializeCategoryTimestamps();
            dataService1.UpdateLastPostedItemTimestamp(ChannelNameEnum.AOS, newTime);

            // Read it back with a new instance
            var dataService2 = new DataService(tempFile, _mockLogger);
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