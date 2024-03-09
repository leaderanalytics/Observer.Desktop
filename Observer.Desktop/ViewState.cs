using LeaderAnalytics.Vyntix.Fred.Model;

namespace LeaderAnalytics.Observer.Desktop;

internal class ViewState
{
    public View View { get; set; }
    public string SearchText { get; set; }
    public string SortExpression { get; set; }
    public bool SortAscending { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public List<LeaderAnalytics.Vyntix.Fred.Domain.Downloader.Node> BreadCrumbs { get; set; } = new(10);

    public void Clear(View view)
    {
        View = view;
        SearchText = SortExpression = null;
        SortAscending = true;
        Skip = Take = 0;
    }
}
