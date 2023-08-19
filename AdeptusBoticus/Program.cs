using AdeptusBoticus;

var feedService = new FeedService("https://www.warhammer-community.com/feed/");

var xmlDoc= await feedService.GetFeedAsync();
Article latestArticle = feedService.GetLatestWarhammerArticle(xmlDoc);
Console.WriteLine("", latestArticle.Id);