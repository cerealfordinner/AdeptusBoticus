namespace AdeptusBoticus.Models;

public class ChannelConfig
{
    public ChannelNameEnum ChannelName { get; set; }
    public ulong ChannelId { get; set; }
    public required List<string> Categories { get; set; }
}