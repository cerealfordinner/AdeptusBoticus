using System.Text.RegularExpressions;

namespace AdeptusBoticus;

public static class StringExtensions
{
    public static int GetArticleIdFromlink(this string link)
    {
        var match = Regex.Match(link, @"(\d+)$");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int articleId))
            {
                return articleId;
        }
        return -1; // Invalid ID
    }

    public static string StripHtmlTags(this string link)
    {
        return Regex.Replace(link, "<.*?>", string.Empty);
    }
}
