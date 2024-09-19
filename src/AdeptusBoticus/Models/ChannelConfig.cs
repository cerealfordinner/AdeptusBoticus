namespace AdeptusBoticus.Models;

public class ChannelConfig
{
    public ChannelNameEnum ChannelName { get; set; }
    public ulong ChannelId { get; set; }
    public List<string> Categories { get; set; }
}