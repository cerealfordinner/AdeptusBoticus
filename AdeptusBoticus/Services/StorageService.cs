namespace AdeptusBoticus;

public class StorageService
{

    private string GetFilePath(string fileName)
    {
        // string dataFolderPath = Path.Combine(AppContext.BaseDirectory, "Data");
        string dataFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Data");
        Directory.CreateDirectory(dataFolderPath);
        return Path.Combine(dataFolderPath, fileName);
    }

    public DateTime GetLastPostedArticleDate()
    {
        string filePath = GetFilePath("lastPostedArticleDate.txt");
        if (File.Exists(filePath) && DateTime.TryParse(File.ReadAllText(filePath), out DateTime lastPostedDate))
        {
            return lastPostedDate;
        }

        // If the date isn't there or valid we'll reset it
        DateTime defaultDateTime = new DateTime(2000, 1, 1);
        UpdateLastPostedArticleDate(defaultDateTime);
        return defaultDateTime;
    }

    public void UpdateLastPostedArticleDate(DateTime newDate)
    {
        string filePath = GetFilePath("lastPostedArticleDate.txt");
        File.WriteAllText(filePath, newDate.ToString("o"));
    }

    public void MarkArticleAsPosted(int articleId)
    {
        string filePath = GetFilePath("lastPostedArticleDate.txt");
    }

    public bool IsArticleAlreadyPosted(int articleId)
    {
        string filePath = GetFilePath("lastPostedArticleDate.txt");
        if (File.Exists(filePath))
        {
            string[] postedArticleIds = File.ReadAllLines(filePath);
            return postedArticleIds.Contains(articleId.ToString());
        }
        return false;
    }
}
