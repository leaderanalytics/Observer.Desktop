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
    private const int MAX_MESSAGE_QUEUE_LEN = 200;
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
    
    public event EventHandler<bool> IsDownloadingChanged;
    public event EventHandler<FredDownloadArgs> DownloadStarted;
    public event EventHandler<FredDownloadArgs> DownloadCompleted;
    public event EventHandler<string> DownloadStatusMessage;
    public Queue<string> Messages { get; set; } = new Queue<string>(MAX_MESSAGE_QUEUE_LEN);

    internal DownloadManager(IAdaptiveClient<IAPI_Manifest> serviceClient, TaskCompletionSource<bool> tcs, ILogger<DownloadManager> logger)
    {
        this.serviceClient = serviceClient;
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

            DateTime startTime = DateTime.Now;
            string startTimeString = startTime.ToString(Constants.DateTimeFormat);
            OnDownloadStarted(args);
            IsDownloading = true;
            Messages.Clear();
            logger.LogInformation("=======================================================================================");
            logger.LogInformation("Download dequed and started at {d}.  Args are: {@args}", startTimeString, args);
            OnDownloadStatusMessage($"Download dequed and started at {startTimeString}.");
            

            try
            {
                await serviceClient.CallAsync(x => x.DownloadService.Download(args, OnDownloadStatusMessage));
            }
            finally
            {
                IsDownloading = false;
                OnDownloadCompleted(args);
            }
            DateTime endTime = DateTime.Now;
            string endTimeString = endTime.ToString(Constants.DateTimeFormat);
            string elapsed = endTime.Subtract(startTime).ToString("hh\\:mm\\:ss");
            OnDownloadStatusMessage($"Download ended at {endTimeString}.  Elapsted time is {elapsed}.");
            logger.LogInformation("Download completed at {d}. Elapsed time is {e}.  Args are: {@args}", endTimeString, elapsed, args);
            logger.LogInformation("=======================================================================================");
        }
        logger.LogDebug("StartQueueProcessing has ended normally.");
        tcs.SetResult(true);
    }

    private void OnIsDownloadingChanged(bool e) => IsDownloadingChanged?.Invoke(this, e);
    private void OnDownloadStarted(FredDownloadArgs e) => DownloadStarted?.Invoke(this, e);
    private void OnDownloadCompleted(FredDownloadArgs e) => DownloadCompleted?.Invoke(this, e);
    
    public void OnDownloadStatusMessage(string msg)
    {
        Messages.Enqueue(msg);
        
        if(Messages.Count > MAX_MESSAGE_QUEUE_LEN)
            Messages.Dequeue();

        DownloadStatusMessage?.Invoke(this, msg);
    }
}
