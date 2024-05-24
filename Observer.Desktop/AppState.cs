using System.Runtime.InteropServices;

namespace LeaderAnalytics.Observer.Desktop;

internal class AppState
{
    internal UserSettings UserSettings { get; set; }
    private readonly UserSettingsService userSettingsService;
    private List<ViewState> ViewStates { get; set; }
    public string ProgramUpdateURL { get; private set; }        // Url to check for updates to this program.
    public OSPlatform OSPlatform { get; private set; }

    internal AppState(UserSettingsService userSettingsService, OSPlatform os, string programUpdateURL)
    {
        this.userSettingsService = userSettingsService ?? throw new ArgumentNullException(nameof(userSettingsService));
        OSPlatform = os;
        ProgramUpdateURL = programUpdateURL;
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
            viewState = new ViewState { View = view, Take = 500, SortAscending = true };
            ViewStates.Add(viewState);
        }
        return viewState;
    }
}
