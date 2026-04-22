namespace AdeptusBoticus.Models;

public class CategoryTracker
{
    public required string ChannelName { get; set; }
    public DateTime LastPostedItemTimeStamp { get; set; }
    public required string LastPostedItemId { get; set; }
    public required string LastPostedItemUuid { get; set; }
}
