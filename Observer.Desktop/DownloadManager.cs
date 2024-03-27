using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace LeaderAnalytics.Observer.Desktop;

internal class DownloadManager
{
    private IAdaptiveClient<IAPI_Manifest> serviceClient;
    private BlockingCollection<FredDownloadArgs> queue;
    private TaskCompletionSource<bool> tcs;
    private readonly ILogger<DownloadManager> logger;
    private bool _IsDownloading;
    internal bool IsDownloading 
    {
        get => _IsDownloading;
        private set
        {
            _IsDownloading = value;
            OnIsDownloadingChanged(value);
        }
    }
    //[Parameter] public Func<bool,Task> IsDownloadingChanged { get; set; }
    public event EventHandler<bool> IsDownloadingChanged;
    public event EventHandler<FredDownloadArgs> DownloadStarted;
    public event EventHandler<FredDownloadArgs> DownloadCompleted;

    internal DownloadManager(IAdaptiveClient<IAPI_Manifest>  client, TaskCompletionSource<bool> tcs, ILogger<DownloadManager> logger)
    {
        this.serviceClient = client ?? throw new ArgumentNullException(nameof(client));
        this.tcs = tcs ?? throw new ArgumentNullException(nameof(tcs));
        this.logger = logger;
        queue = new BlockingCollection<FredDownloadArgs>();
    }

    internal void QueueDownload(FredDownloadArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        queue.TryAdd(args);
        logger.LogDebug("FredDownloadArgs queued for args: {@args}", args);
    }

    internal void StopProcessing()
    {
        queue.CompleteAdding();
        logger.LogDebug("queue.CompleteAdding() was called.");
    }

    /// <summary>
    /// This method is called as a fire-and-forget task in Program.cs.  Set TaskCompletionSource here to notify 
    /// any methods awaiting this task that all downloads are complete.
    /// </summary>
    /// <returns>Task</returns>
    internal async Task StartQueueProcessing()
    {
        while (!queue.IsCompleted)
        {
            if (!queue.TryTake(out FredDownloadArgs args, -1))
                break;

            logger.LogInformation("Download dequed and started.  Args are: {@args}", args);
            IsDownloading = true;
            OnDownloadStarted(args);

            try
            {
                await serviceClient.CallAsync(x => x.DownloadService.Download(args));
            }
            finally
            {
                IsDownloading = false;
                OnDownloadCompleted(args);
            }
            
            logger.LogInformation("Download completed.  Args are: {@args}", args);
        }
        logger.LogDebug("StartQueueProcessing has ended normally.");
        tcs.SetResult(true);
    }

    private void OnIsDownloadingChanged(bool e) => IsDownloadingChanged?.Invoke(this, e);
    private void OnDownloadStarted(FredDownloadArgs e) => DownloadStarted?.Invoke(this, e);
    private void OnDownloadCompleted(FredDownloadArgs e) => DownloadCompleted?.Invoke(this, e);
    
}
