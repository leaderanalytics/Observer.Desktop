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

    public DownloadManager(IAdaptiveClient<IAPI_Manifest>  client, TaskCompletionSource<bool> tcs, ILogger<DownloadManager> logger)
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

    public async Task StartQueueProcessing()
    {
        while(! queue.IsCompleted) 
        {
            if (!queue.TryTake(out FredDownloadArgs args, -1))
                break;

            logger.LogInformation("Download dequed and started.  Args are: {@args}", args);
            await serviceClient.CallAsync(x => x.DownloadService.Download(args));
            logger.LogInformation("Download completed.  Args are: {@args}", args);
        }
        logger.LogDebug("StartQueueProcessing has ended normally.");
        tcs.SetResult(true);
    }
}
