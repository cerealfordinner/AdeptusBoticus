using System.Xml.Linq;
using AdeptusBoticus;

var feedService = new FeedService("https://www.warhammer-community.com/feed/");

Task<XDocument> xmlDocTask = feedService.GetFeedAsync();
var xmlDoc = xmlDocTask.Result;
Article latestArticle = feedService.GetLatestWarhammerArticle(xmlDoc);