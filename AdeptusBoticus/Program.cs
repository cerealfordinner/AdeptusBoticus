using AdeptusBoticus;

var webScrapingService = new WebScrapingService("https://www.warhammer-community.com/en-us/warhammer-40000/");
webScrapingService.CheckForNewArticles();