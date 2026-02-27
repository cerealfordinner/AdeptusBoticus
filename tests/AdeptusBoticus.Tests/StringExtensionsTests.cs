using AdeptusBoticus.Extensions;

namespace AdeptusBoticus.Tests;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("<p>Hello World</p>", "Hello World")]
    [InlineData("<h1>Title</h1>", "Title")]
    [InlineData("No tags here", "No tags here")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("<div><p>Nested</p></div>", "Nested")]
    [InlineData("Text with &lt;escaped&gt; tags", "Text with escaped tags")]
    public void StripHtmlTags_RemovesHtmlTags(string? input, string expected)
    {
        var result = input.StripHtmlTags();
        
        Assert.Equal(expected, result);
    }
}