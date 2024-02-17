using System.Xml.Linq;

namespace AdeptusBoticus.Models;

public abstract class IArticle
{
    public string? Title { get; set; }
    public string? Link { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public DateTime PublicationDate { get; set; }
    
}

class Warhammer40KArticle : IArticle
{
}

class HorusHersyArticle : IArticle
{
}

class FantasyArticle : IArticle
{
}
