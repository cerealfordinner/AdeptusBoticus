using System.Globalization;
using System.Xml.Linq;

namespace AdeptusBoticus.Helpers;

public static class DateTimeHelper
{
    public static DateTime GetPublicationDateFromElement(XElement articleElement)
    {
        string? dateString = articleElement.Element("pubDate")?.Value;
        if (DateTime.TryParseExact(dateString, "ddd, dd MMM yyyy HH:mm:ss zzz",
                                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime publicationDate))
        {
            return publicationDate;
        }
        
        return DateTime.MinValue;
    }
}
