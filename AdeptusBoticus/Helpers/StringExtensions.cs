using System.Text.RegularExpressions;

namespace AdeptusBoticus.Helpers;

public static class StringExtensions
{
    public static string StripHtmlTags(this string? link)
    {
        return Regex.Replace(link, "<.*?>", string.Empty);
    }
}
