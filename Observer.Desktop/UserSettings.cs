using LeaderAnalytics.Vyntix.FileExporters;
namespace LeaderAnalytics.Observer.Desktop;

internal class UserSettings
{
    public bool DarkTheme { get; set; }  // must be public or it wont serialize
    public ExportSettings ExportSettings { get; set; }
    public FredDownloadArgs CategoryPathDownloadArgs { get; set; }
    public FredDownloadArgs SeriesPathDownloadArgs { get; set; }
}

internal class ExportSettings
{
    // Can't save Initial vintage only because it is series dependent.
    
    public DataLayout DataLayout { get; set; }
    public FileFormat FileFormat { get; set; }
    public SortPriority SortPriority { get; set; }
    public LeaderAnalytics.Vyntix.FileExporters.SortDirection ObsSortDirection { get; set; }
    public LeaderAnalytics.Vyntix.FileExporters.SortDirection VintSortDirection { get; set; }
}