using System;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

namespace PSMWUpdater.Infrastructures
{
    public abstract class PSAsyncCmdlet : PSCmdlet
    {

        private CancellationTokenSource cts;

        private void RunAsync(Func<Task> taskFunc)
        {
            using (var syncContext = new QueueSynchronizationContext())
            {
                SynchronizationContext.SetSynchronizationContext(syncContext);
                try
                {
                    var task = taskFunc();
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
        protected override void BeginProcessing()
        {
            cts = new CancellationTokenSource();
        }

        /// <inheritdoc />
        protected sealed override void ProcessRecord()
        {
            RunAsync(() => ProcessRecordAsync(cts.Token));
        }

        /// <inheritdoc />
        protected sealed override void StopProcessing()
        {
            cts.Cancel();
        }

        /// <inheritdoc />
        protected sealed override void EndProcessing()
        {
            RunAsync(() => EndProcessingAsync(cts.Token));
            cts.Dispose();
            cts = null;
        }

        protected abstract Task ProcessRecordAsync(CancellationToken cancellationToken);

        protected virtual Task EndProcessingAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

    }
}
