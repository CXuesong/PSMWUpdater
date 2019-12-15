using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using PSMWUpdater.Infrastructures;
using WikiClientLibrary.Sites;

namespace PSMWUpdater.Commands
{

    /// <summary>
    /// Gets a list of extensions either from a local MediaWiki installation or from WMF-registered MediaWiki extensions.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, NounsCommon.MwExtension)]
    [OutputType(typeof(ExtensionInfo), typeof(LocalExtensionInfo))]
    public class GetMwExtensionCommand : PSAsyncCmdlet
    {

        /// <summary>
        /// If specified, gets all the extensions from the local MediaWiki installation in the specified path.
        /// </summary>
        [Parameter(Position = 0)]
        public string InstallationPath { get; set; }

        /// <inheritdoc />
        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            if (InstallationPath == null)
            {
                var site = await AmbientServices.GetExtensionProviderSiteAsync();
                var names = await site.GetKnownExtensionsAsync(cancellationToken);
                foreach (var name in names)
                    WriteObject(new ExtensionInfo(name));
            }
            else
            {
                var resolvedPath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(InstallationPath);
                if (!MediaWikiInstallation.CheckMwRootFolder(resolvedPath))
                    throw new ArgumentException("Specified path is not a valid MediaWiki installation.", nameof(InstallationPath));
                var installation = new MediaWikiInstallation(resolvedPath);
                installation.Refresh();
                WriteObject(installation.InstalledExtensions, true);
                WriteObject(installation.InstalledSkins, true);
            }
        }
    }

}
