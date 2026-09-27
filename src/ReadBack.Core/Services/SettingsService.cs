using System.Text.Json;
using ReadBack.Core.Models;

namespace ReadBack.Core.Services;

public class SettingsService : ISettingsService
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ReadBack"
    );

    private static readonly string SettingsFile = Path.Combine(SettingsDir, "settings.json");

    public AppSettings CurrentSettings { get; private set; }

    public event EventHandler<AppSettings>? SettingsChanged;

    public SettingsService()
    {
        CurrentSettings = Load();
    }

    private AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                string json = File.ReadAllText(SettingsFile);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null) return loaded;
            }
        }
        catch { }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            string json = JsonSerializer.Serialize(CurrentSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
            SettingsChanged?.Invoke(this, CurrentSettings);
        }
        catch { }
    }
}
