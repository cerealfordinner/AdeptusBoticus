using AdeptusBoticus.Models;

namespace AdeptusBoticus.Tests;

public class CategoryTrackerTests
{
    [Fact]
    public void CategoryTracker_SetsPropertiesCorrectly()
    {
        var tracker = new CategoryTracker
        {
            ChannelName = "WH40K",
            LastPostedItemUuid = "test-uuid"
        };

        Assert.Equal("WH40K", tracker.ChannelName);
        Assert.Equal("test-uuid", tracker.LastPostedItemUuid);
    }
}