using AdeptusBoticus;
using Microsoft.Extensions.Configuration;

IConfiguration configuration = new ConfigurationBuilder()
.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
.AddJsonFile("appsettings.json")
.AddJsonFile("appsettings.dev.json", optional : true, reloadOnChange: true)
.Build();

var FeedService = new FeedService(configuration);
await FeedService.RunAsync();
