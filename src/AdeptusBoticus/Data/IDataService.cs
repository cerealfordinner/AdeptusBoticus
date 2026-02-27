using AdeptusBoticus.Models;
using MongoDB.Driver;

namespace AdeptusBoticus.Data;

public interface IDataService
{
    IMongoCollection<CategoryTracker> GetCategoryTrackers();
    void UpdateLastPostedItemTimestamp(ChannelNameEnum channelName, DateTime time);
    void InitializeCategoryTimestamps();
}
