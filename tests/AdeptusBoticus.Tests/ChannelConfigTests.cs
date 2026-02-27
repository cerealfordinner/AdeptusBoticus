using AdeptusBoticus.Models;

namespace AdeptusBoticus.Tests;

public class ChannelConfigTests
{
    [Fact]
    public void ChannelConfig_SetsPropertiesCorrectly()
    {
        var config = new ChannelConfig
        {
            ChannelName = ChannelNameEnum.WH40K,
            ChannelId = 123456789UL,
            Categories = ["Warhammer 40000", "40k"]
        };

        Assert.Equal(ChannelNameEnum.WH40K, config.ChannelName);
        Assert.Equal(123456789UL, config.ChannelId);
        Assert.Equal(2, config.Categories.Count);
        Assert.Contains("Warhammer 40000", config.Categories);
    }

    [Fact]
    public void ChannelConfig_RequiresCategories()
    {
        var config = new ChannelConfig
        {
            ChannelName = ChannelNameEnum.AOS,
            ChannelId = 987654321UL,
            Categories = ["AoS"]
        };

        Assert.NotNull(config.Categories);
    }
}