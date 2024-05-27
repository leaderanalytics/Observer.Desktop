using System.Text.Json;

namespace LeaderAnalytics.Observer.Desktop;
internal class UserSettingsService
{
    private static string ConfigFilePath;
    private static string _SettingsFileName;
    private static string SettingsFileName
    {
        get
        { 
            if(_SettingsFileName == null)
                _SettingsFileName = Path.Combine(ConfigFilePath, "UserSettings.json");

            return _SettingsFileName;
        }
    }

    public UserSettingsService(string configFilePath)
    {
        ConfigFilePath = configFilePath ?? throw new Exception("configFilePath is required.");
    }

    internal UserSettings GetUserSettings()
    {
        if (!File.Exists(SettingsFileName))
            return DefaultSettings();
        
        UserSettings userSettings;

        try
        {
            userSettings = JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(SettingsFileName));
        }
        catch 
        {
            // Return defaults if we hit an error deserializing an outdated settings file.
            userSettings = DefaultSettings();
        }
        
        // check reference properties
        if (userSettings?.ExportSettings is null)
            userSettings = DefaultSettings();

        return userSettings;
    }
    
    internal void SaveUserSettings(UserSettings userSettings)
    {
        ArgumentNullException.ThrowIfNull(userSettings);
        string json = JsonSerializer.Serialize(userSettings);
        File.WriteAllText(SettingsFileName, json);
    }

    private UserSettings DefaultSettings()
    {
        return new UserSettings
        {
            DarkTheme = true,
            ExportSettings = new ExportSettings
            {
                DataLayout = DataLayout.Matrix,
                FileFormat = FileFormat.Excel,
                SortPriority = SortPriority.ObservationDate,
                ObsSortDirection = Vyntix.FileExporters.SortDirection.Descending,
                VintSortDirection = Vyntix.FileExporters.SortDirection.Descending
            }
        };
    }
}
