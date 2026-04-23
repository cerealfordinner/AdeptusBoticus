using AdeptusBoticus.Models;

namespace AdeptusBoticus.Data;

public interface IDataService
{
    CategoryTracker? GetTracker(ChannelNameEnum channelName);
    string? GetLastPostedUuid(ChannelNameEnum channelName);
    void UpdateLastPostedItemUuid(ChannelNameEnum channelName, string itemUuid);
    void InitializeCategoryTrackers();
}
