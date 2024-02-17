namespace LeaderAnalytics.Observer.Desktop;

internal class AppState
{
    internal UserSettings UserSettings { get; set; }
    private readonly UserSettingsService userSettingsService;

    internal AppState(UserSettingsService userSettingsService)
    {
        this.userSettingsService = userSettingsService ?? throw new ArgumentNullException(nameof(userSettingsService));
        LoadUserSettings();
    }

    internal void LoadUserSettings() => UserSettings = userSettingsService.GetUserSettings();
    internal void SaveUserSettings() => userSettingsService.SaveUserSettings(UserSettings);
}
