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

    public string? GetLastPostedUuid(ChannelNameEnum channelName)
    {
        lock (_lock)
        {
            var tracker = _trackers.FirstOrDefault(t =>
                t.ChannelName.Equals(channelName.ToString(), StringComparison.OrdinalIgnoreCase));
            return tracker?.LastPostedItemUuid;
        }
    }

    public void UpdateLastPostedItemUuid(ChannelNameEnum channelName, string itemUuid)
    {
        lock (_lock)
        {
            var tracker = _trackers.FirstOrDefault(t =>
                t.ChannelName.Equals(channelName.ToString(), StringComparison.OrdinalIgnoreCase));

            if (tracker != null)
            {
                tracker.LastPostedItemUuid = itemUuid;
            }
            else
            {
                _trackers.Add(new CategoryTracker
                {
                    ChannelName = channelName.ToString(),
                    LastPostedItemUuid = itemUuid
                });
            }

            SaveToFile();
        }

        _logger.LogDebug("Updated UUID for {ChannelName} to {ItemUuid}", channelName, itemUuid);
    }

    public void InitializeCategoryTrackers()
    {
        lock (_lock)
        {
            var channelNames = Enum.GetNames<ChannelNameEnum>();
            var missingChannels = channelNames.Where(name =>
                !_trackers.Any(t => t.ChannelName.Equals(name, StringComparison.OrdinalIgnoreCase))).ToList();

            if (missingChannels.Any())
            {
                foreach (var channelName in missingChannels)
                {
                    _trackers.Add(new CategoryTracker
                    {
                        ChannelName = channelName,
                        LastPostedItemUuid = string.Empty
                    });
                    _logger.LogInformation("Created new tracker for {ChannelName}", channelName);
                }

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
            var oldTrackers = JsonSerializer.Deserialize<List<CategoryTracker>>(json, JsonOptions);

            if (oldTrackers != null && oldTrackers.Any())
            {
                // Check if old format (has timestamp or id fields)
                var hasOldFormat = oldTrackers.Any(t =>
                    t.GetType().GetProperty("LastPostedItemTimeStamp") != null ||
                    t.GetType().GetProperty("LastPostedItemId") != null);

                if (hasOldFormat)
                {
                    _logger.LogInformation("Found old format trackers.json. Converting to new format.");

                    // Convert to new format
                    _trackers = oldTrackers.Select(oldTracker => new CategoryTracker
                    {
                        ChannelName = oldTracker.ChannelName,
                        LastPostedItemUuid = oldTracker.LastPostedItemUuid ?? string.Empty
                    }).ToList();

                    SaveToFile();
                    return;
                }

                _logger.LogInformation("Found existing trackers.json with {Count} entries.", oldTrackers.Count);
                _trackers = oldTrackers;
                return;
            }

            _trackers = [];
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
