using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

namespace PSMWUpdater.Infrastructures
{
    public abstract class PSAsyncCmdlet : PSCmdlet
    {

        private CancellationTokenSource cts;

        /// <inheritdoc />
        protected override void BeginProcessing()
        {
            cts = new CancellationTokenSource();
        }

        /// <inheritdoc />
        protected sealed override void ProcessRecord()
        {
            using (var syncContext = new QueueSynchronizationContext())
            {
                SynchronizationContext.SetSynchronizationContext(syncContext);
                try
                {
                    var task = ProcessRecordAsync(cts.Token);
                    using (var syncCts = new CancellationTokenSource())
                    {
                        task.ContinueWith(t => syncCts.Cancel(), syncCts.Token);
                        syncContext.Run(syncCts.Token);
                        task.GetAwaiter().GetResult();
                    }
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(null);
                }
            }
        }

        /// <inheritdoc />
        protected sealed override void StopProcessing()
        {
            cts.Cancel();
        }

        /// <inheritdoc />
        protected override void EndProcessing()
        {
            cts.Dispose();
            cts = null;
        }

        protected abstract Task ProcessRecordAsync(CancellationToken cancellationToken);

    }
}
