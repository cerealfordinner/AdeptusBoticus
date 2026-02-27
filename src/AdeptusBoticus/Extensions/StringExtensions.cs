using System.Net;
using System.Text.RegularExpressions;

namespace AdeptusBoticus.Extensions;

public static class StringExtensions
{
    public static string StripHtmlTags(this string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var decoded = WebUtility.HtmlDecode(input);
        return Regex.Replace(decoded, "<.*?>", string.Empty);
    }
}