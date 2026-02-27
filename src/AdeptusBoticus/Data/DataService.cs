using System.Text.Json;
using AdeptusBoticus.Models;
using Microsoft.Extensions.Logging;

namespace AdeptusBoticus.Data;

public class DataService : IDataService
{
    private readonly string _filePath;
    private readonly ILogger<DataService> _logger;
    private readonly object _lock = new();
    private List<CategoryTracker> _trackers = [];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public DataService(string filePath, ILogger<DataService> logger)
    {
        _logger = logger;
        _filePath = filePath;
        LoadFromFile();
    }

    public CategoryTracker? GetTracker(ChannelNameEnum channelName)
    {
        lock (_lock)
        {
            return _trackers.FirstOrDefault(t =>
                t.ChannelName.Equals(channelName.ToString(), StringComparison.OrdinalIgnoreCase));
        }
    }

    public void UpdateLastPostedItemTimestamp(ChannelNameEnum channelName, DateTime time)
    {
        lock (_lock)
        {
            var tracker = _trackers.FirstOrDefault(t =>
                t.ChannelName.Equals(channelName.ToString(), StringComparison.OrdinalIgnoreCase));

            if (tracker != null)
            {
                tracker.LastPostedItemTimeStamp = time;
            }
            else
            {
                _trackers.Add(new CategoryTracker
                {
                    ChannelName = channelName.ToString(),
                    LastPostedItemTimeStamp = time
                });
            }

            SaveToFile();
        }

        _logger.LogDebug("Updated timestamp for {ChannelName} to {Time}", channelName, time);
    }

    public void InitializeCategoryTimestamps()
    {
        lock (_lock)
        {
            var changed = false;

            foreach (ChannelNameEnum channel in Enum.GetValues(typeof(ChannelNameEnum)))
            {
                var exists = _trackers.Any(t =>
                    t.ChannelName.Equals(channel.ToString(), StringComparison.OrdinalIgnoreCase));

                if (!exists)
                {
                    _trackers.Add(new CategoryTracker
                    {
                        ChannelName = channel.ToString(),
                        LastPostedItemTimeStamp = DateTime.UtcNow
                    });
                    changed = true;
                    _logger.LogInformation("Created new tracker for {ChannelName}", channel);
                }
            }

            if (changed)
            {
                SaveToFile();
            }
        }
    }

    private void LoadFromFile()
    {
        if (!File.Exists(_filePath))
        {
            _logger.LogInformation("Data file not found at {FilePath}, starting with empty state", _filePath);
            _trackers = [];
            return;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            _trackers = JsonSerializer.Deserialize<List<CategoryTracker>>(json, JsonOptions) ?? [];
            _logger.LogInformation("Loaded {Count} trackers from {FilePath}", _trackers.Count, _filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load data file at {FilePath}, starting with empty state", _filePath);
            _trackers = [];
        }
    }

    private void SaveToFile()
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_trackers, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save data file to {FilePath}", _filePath);
        }
    }
}
