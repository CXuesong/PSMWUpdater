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
    public class GetMwExtensionCommand : AsyncCmdlet
    {

        /// <summary>
        /// If specified, gets all the extensions from the local MediaWiki installation in the specified path.
        /// </summary>
        [Parameter(Position = 0)]
        public FileInfo InstallationPath { get; set; }

        /// <inheritdoc />
        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            var site = await AmbientServices.GetExtensionProviderSiteAsync();
            if (InstallationPath == null)
            {
                var names = await site.GetKnownExtensionsAsync(cancellationToken);
                foreach (var name in names)
                    WriteObject(new ExtensionInfo(name));
            }
            else
            {
                if (!MediaWikiInstallation.CheckMwRootFolder(InstallationPath.FullName))
                    throw new ArgumentException("Specified folder is not a valid MediaWiki installation.", nameof(InstallationPath));
                var installation = new MediaWikiInstallation(InstallationPath.FullName);
                WriteObject(installation.InstalledExtensions, true);
                WriteObject(installation.InstalledSkins, true);
            }
        }
    }

}
