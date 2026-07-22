using System.Text.Json;

namespace Databasemanager;

public static class ConfigurationService
{
    private static readonly string SettingsDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Databasemanager");

    private static readonly string SettingsFile =
        Path.Combine(SettingsDirectory, "settings.json");

    public static string SettingsFilePath => SettingsFile;

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFile))
            {
                var defaultSettings = EnsureDefaultDirectories(new AppSettings());
                Save(defaultSettings);
                return defaultSettings;
            }

            string json = File.ReadAllText(SettingsFile);
            var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            return EnsureDefaultDirectories(settings);
        }
        catch
        {
            return EnsureDefaultDirectories(new AppSettings());
        }
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsDirectory);
        Directory.CreateDirectory(settings.SqlScriptsPath);
        Directory.CreateDirectory(settings.OutputDirectory);

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(SettingsFile, JsonSerializer.Serialize(settings, options));
    }

    private static AppSettings EnsureDefaultDirectories(AppSettings settings)
    {
        Directory.CreateDirectory(settings.SqlScriptsPath);
        Directory.CreateDirectory(settings.OutputDirectory);
        return settings;
    }
}
