using System.Text.RegularExpressions;

namespace AdeptusBoticus.Extensions;

public static class StringExtensions
{
    public static string StripHtmlTags(this string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return Regex.Replace(input, "<.*?>", string.Empty);
    }
}