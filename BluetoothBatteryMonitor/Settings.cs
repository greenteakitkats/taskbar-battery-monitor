using System.Text.Json;

namespace XboxControllerBattery;

/// <summary>User-configurable settings, persisted to %LocalAppData%\XboxControllerBattery\settings.json.</summary>
public sealed class Settings
{
    public int LowBatteryThreshold { get; set; } = 20;
    public int PollIntervalSeconds { get; set; } = 30;
    public bool LowBatteryWarningEnabled { get; set; } = true;

    /// <summary>Stable Id of the device whose battery is drawn on the tray icon.
    /// Null means "auto" — follow whichever device has the lowest battery.</summary>
    public string? PrimaryDeviceId { get; set; }

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "XboxControllerBattery", "settings.json");

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var loaded = JsonSerializer.Deserialize<Settings>(json);
                if (loaded != null)
                    return loaded;
            }
        }
        catch
        {
            // Corrupt or unreadable file -> fall back to defaults.
        }
        return new Settings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Non-fatal: settings just won't persist this run.
        }
    }
}
