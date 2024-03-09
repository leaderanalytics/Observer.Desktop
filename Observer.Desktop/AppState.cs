namespace LeaderAnalytics.Observer.Desktop;

internal class AppState
{
    internal UserSettings UserSettings { get; set; }
    private readonly UserSettingsService userSettingsService;
    private List<ViewState> ViewStates { get; set; }



    internal AppState(UserSettingsService userSettingsService)
    {
        this.userSettingsService = userSettingsService ?? throw new ArgumentNullException(nameof(userSettingsService));
        LoadUserSettings();
        ViewStates = new();
    }

    internal void LoadUserSettings() => UserSettings = userSettingsService.GetUserSettings();
    internal void SaveUserSettings() => userSettingsService.SaveUserSettings(UserSettings);



    internal ViewState GetViewState(View view)
    {
        ViewState viewState = ViewStates.FirstOrDefault(x => x.View == view);

        if (viewState is null)
        {
            viewState = new ViewState { View = view };
            ViewStates.Add(viewState);
        }
        return viewState;
    }
}
