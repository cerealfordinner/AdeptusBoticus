using AdeptusBoticus.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AdeptusBoticus.Data;

public class DataService : IDataService
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<DataService> _logger;

    public DataService(string mongoUri, ILogger<DataService> logger)
    {
        _logger = logger;
        var mongoClient = new MongoClient(mongoUri);
        _database = mongoClient.GetDatabase("adeptusboticus_db");
    }

    public IMongoCollection<CategoryTracker> GetCategoryTrackers()
    {
        return _database.GetCollection<CategoryTracker>("category_trackers");
    }

    public void UpdateLastPostedItemTimestamp(ChannelNameEnum channelName, DateTime time)
    {
        var collection = GetCategoryTrackers();
        var filter = Builders<CategoryTracker>.Filter.Eq(c => c.ChannelName, channelName.ToString());
        var update = Builders<CategoryTracker>.Update.Set(c => c.LastPostedItemTimeStamp, time);

        collection.UpdateOne(filter, update, new UpdateOptions { IsUpsert = true });
        _logger.LogDebug("Updated timestamp for {ChannelName} to {Time}", channelName, time);
    }

    public void InitializeCategoryTimestamps()
    {
        var collection = GetCategoryTrackers();
        foreach (ChannelNameEnum channel in Enum.GetValues(typeof(ChannelNameEnum)))
        {
            var filter = Builders<CategoryTracker>.Filter.Eq(c => c.ChannelName, channel.ToString());
            var exists = collection.Find(filter).FirstOrDefault();

            if (exists == null)
            {
                var newTracker = new CategoryTracker
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ChannelName = channel.ToString(),
                    LastPostedItemTimeStamp = DateTime.UtcNow
                };
                collection.InsertOne(newTracker);
                _logger.LogInformation("Created new tracker for {ChannelName}", channel);
            }
        }
    }
}
