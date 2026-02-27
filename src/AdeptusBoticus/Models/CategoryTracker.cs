using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AdeptusBoticus.Models;

public class CategoryTracker
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public required string Id { get; set; }

    [BsonElement("channel_name")] public required string ChannelName { get; set; }

    [BsonElement("last_posted_item_timestamp")]
    public DateTime LastPostedItemTimeStamp { get; set; }
}