namespace AdeptusBoticus;

public class StorageService
{

    private string GetFilePath(string fileName)
    {
        string dataFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Data");
        Directory.CreateDirectory(dataFolderPath);
        return Path.Combine(dataFolderPath, fileName);
    }

    public DateTime GetLastPostedArticleDate(ArticleType articleType)
    {
        string filePath = string.Empty;
        switch (articleType)
        {
            case ArticleType.Warhammer40K:
            filePath = GetFilePath("40k.txt");
            break;
            case ArticleType.WarhammerHorusHeresy:
            filePath = GetFilePath("hh.txt");
            break;
            case ArticleType.WarhammerFantasy:
            filePath = GetFilePath("fantasy.txt");
            break;
        }

        if (File.Exists(filePath) && DateTime.TryParse(File.ReadAllText(filePath), out DateTime lastPostedDate))
        {
            return lastPostedDate;
        }

        // If the date isn't there or valid we'll reset it
        DateTime defaultDateTime = new DateTime(2000, 1, 1);
        ResetTimeStamp(filePath);
        return defaultDateTime;
    }

    public void ResetTimeStamp(string filePath)
    {
        File.WriteAllText(filePath, new DateTime(2000, 1, 1).ToString("o"));
    }

    public void UpdateLastPostedArticleDate(Article article)
    {
        string fileName = string.Empty;
        switch (article.ArticleType)
        {
            case ArticleType.Warhammer40K:
            fileName = "40k.txt";
            break;
            case ArticleType.WarhammerHorusHeresy:
            fileName = "hh.txt";
            break;
            case ArticleType.WarhammerFantasy:
            fileName = "fantasy.txt";
            break;
        }
        string filePath = GetFilePath(fileName);
        File.WriteAllText(filePath, article.PublicationDate.ToString("o"));
    }

}
