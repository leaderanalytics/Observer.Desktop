using DocumentFormat.OpenXml.Spreadsheet;
using LeaderAnalytics.Vyntix.Fred.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeaderAnalytics.Observer.Desktop.Pages;
public class SeriesViewBasePage : BasePage
{
    internal DownloadManager downloadManager { get; set; }
    protected ILogger<SeriesViewBasePage> logger { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        ILifetimeScope scope = container.BeginLifetimeScope();
        downloadManager = scope.Resolve<DownloadManager>();
    }

    protected async Task<bool> AddSeries() => await ShowDialog("Enter symbols and select related data objects to download", new DialogParameters<SeriesPathDownloadDialog>());

    protected async Task<bool> UpdateAllSeries()
    {
        DialogParameters<SeriesPathDownloadDialog> parameters = new DialogParameters<SeriesPathDownloadDialog>
        {
            { x => x.SymbolDisplayOption, "Hidden" },
            { x => x.symbols, "*" }
        };
        return await ShowDialog("Select related data objects to download for all symbols", parameters);
    }

    protected async Task<bool> UpdateSelectedSeries(IEnumerable<string> selectedItems)
    {
        if ((selectedItems?.Count() ?? 0) == 0)
        {
            await MessageBox.Info("Select one or more series to update.");
            return false;
        }
        
        DialogParameters<SeriesPathDownloadDialog> parameters = new DialogParameters<SeriesPathDownloadDialog>
        {
            { x => x.SymbolDisplayOption, "Disabled" },
            { x => x.symbols, string.Join(',', selectedItems) }
        };
        return await ShowDialog("Select related data objects to download for selected symbols", parameters);
    }

    private async Task<bool> ShowDialog(string caption, DialogParameters<SeriesPathDownloadDialog> parameters)
    {
        DialogOptions options = new DialogOptions { DisableBackdropClick = true, MaxWidth = MaxWidth.Large };
        var result = await DialogService.Show<SeriesPathDownloadDialog>(caption, parameters, options).Result;

        if (!result.Cancelled)
        {
            FredDownloadArgs args = result.Data as FredDownloadArgs;
            logger.LogInformation("Series path download started.  Args are: {@args}", args);
            downloadManager.QueueDownload(args);
            logger.LogInformation("Series path download completed.");
        }
        return !result.Cancelled;
    }

    protected async Task<bool> DeleteSeries(IEnumerable<string> selectedItems)
    {
        if ((selectedItems?.Count() ?? 0) == 0)
        {
            await MessageBox.Info("Select one or more series to delete.");
            return false;
        }
        bool confirm = await MessageBox.Ask($"Are you sure you want to delete {selectedItems.Count()} series?");

        if (confirm)
        {
            await MessageBox.ShowLoading();
            logger.LogInformation("Symbols deleted from database : {@symbols}", selectedItems);

            foreach (string s in selectedItems)
                await serviceClient.CallAsync(x => x.SeriesService.DeleteSeries(s));

            
            await MessageBox.HideLoading();
        }
        return confirm;
    }

    private async Task UpdateSeries(IEnumerable<string> symbols)
    {
        await MessageBox.ShowLoading();

        foreach (string symbol in symbols)
        {
            Snackbar.Add($"Updating series {symbol}...", Severity.Info);
            LeaderAnalytics.Vyntix.Elements.RowOpResult result = await serviceClient.CallAsync(x => x.ObservationsService.DownloadObservations(symbol, null));

            if (result.Success)
                Snackbar.Add($"Series {symbol} was updated successfully.", Severity.Success);
            else
                Snackbar.Add($"Eror updating series {symbol}:  {result.Message}", Severity.Error);

        }
        await MessageBox.HideLoading();
    }
}
