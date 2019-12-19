using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;
using System.Threading;

namespace PSMWUpdater.Infrastructures
{

    public class PSSyncCmdlet : PSCmdlet, IDisposable
    {

        private CancellationTokenSource stopProc;

        protected CancellationToken StopProcessingCancellationToken => stopProc.Token;

        /// <inheritdoc />
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            stopProc?.Dispose();
            stopProc = new CancellationTokenSource();
        }

        /// <inheritdoc />
        protected override void StopProcessing()
        {
            base.StopProcessing();
            stopProc.Cancel();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                stopProc?.Dispose();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

}
