using System.Text.RegularExpressions;

namespace AdeptusBoticus.Extensions;

public static class StringExtensions
{
    public static string StripHtmlTags(this string? link)
    {
        return Regex.Replace(link, "<.*?>", string.Empty);
    }
}