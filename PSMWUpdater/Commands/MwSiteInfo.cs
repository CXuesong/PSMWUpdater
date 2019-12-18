using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PSMWUpdater.Infrastructures;

namespace PSMWUpdater.Commands
{

    /// <summary>
    /// Gets the basic information of a MediaWiki installation.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, NounsCommon.MwSiteInfo)]
    [OutputType(typeof(LocalSiteInfo))]
    public class GetMwSiteInfoCommand : PSAsyncCmdlet
    {

        /// <summary>
        /// Specify the root of a local MediaWiki installation.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public string InstallationPath { get; set; }

        /// <inheritdoc />
        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            var resolvedPath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(InstallationPath);
            MediaWikiInstallation.AssertMwRootFolder(resolvedPath, nameof(InstallationPath));
            var installation = new MediaWikiInstallation(resolvedPath);
            var info = await installation.GetSiteInfoAsync(this, cancellationToken);
            WriteObject(info);
        }

    }

}
