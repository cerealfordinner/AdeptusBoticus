using AdeptusBoticus.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AdeptusBoticus.Data;

public class DataService
{
    private readonly IMongoDatabase _database;

    public DataService()
    {
        var mongoUri = "mongodb://mongodb:27017";
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

        // If the document doesn't exist, upsert will create it
        collection.UpdateOne(filter, update, new UpdateOptions { IsUpsert = true });
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
            }
        }
    }
}