﻿using LeaderAnalytics.Vyntix.Fred.Model;

namespace LeaderAnalytics.Observer.Desktop;

internal class ViewState
{
    public View View { get; set; }
    public string SearchSymbol { get; set; }
    public string SearchTitle { get; set; }
    public string SortExpression { get; set; }
    public bool SortAscending { get; set; }
    public int Page { get; set; }
    public int Skip => Page * Take;
    public int Take { get; set; }
    public List<LeaderAnalytics.Vyntix.Fred.Domain.Downloader.Node> BreadCrumbs { get; set; } = new(10);

    public void Clear(View view)
    {
        View = view;
        SearchTitle = SortExpression = null;
        SortAscending = true;
        Page = 0;
        Take = 500;
    }
}
