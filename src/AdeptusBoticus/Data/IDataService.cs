using AdeptusBoticus.Models;

namespace AdeptusBoticus.Data;

public interface IDataService
{
    CategoryTracker? GetTracker(ChannelNameEnum channelName);
    void UpdateLastPostedItemTimestamp(ChannelNameEnum channelName, DateTime time);
    void InitializeCategoryTimestamps();
}
