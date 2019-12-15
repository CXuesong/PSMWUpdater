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
        /// If specified, gets all the extensions from the local MediaWiki installation in the specified path,
        /// instead of retrieving known extensions from server.
        /// </summary>
        [Parameter(Position = 0)]
        public string InstallationPath { get; set; }

        /// <summary>
        /// Only list extensions of this type.
        /// </summary>
        [Parameter]
        [ValidateSet("All", nameof(ExtensionType.Extension), nameof(ExtensionType.Skin), IgnoreCase = true)]
        public string Type { get; set; }

        /// <inheritdoc />
        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            var filterType = Type == null || "All".Equals(Type, StringComparison.OrdinalIgnoreCase)
                ? ExtensionType.Unknown
                : (ExtensionType)Enum.Parse(typeof(ExtensionType), Type, true);
            if (InstallationPath == null)
            {
                var site = await AmbientServices.GetExtensionProviderSiteAsync();
                var names = await site.GetKnownExtensionsAsync(cancellationToken);
                foreach (var name in names)
                {
                    if (filterType != ExtensionType.Unknown && name.Type != filterType)
                        continue;
                    WriteObject(new ExtensionInfo(name));
                }
            }
            else
            {
                var resolvedPath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(InstallationPath);
                if (!MediaWikiInstallation.CheckMwRootFolder(resolvedPath))
                    throw new ArgumentException("Specified path is not a valid MediaWiki installation.", nameof(InstallationPath));
                var installation = new MediaWikiInstallation(resolvedPath);
                installation.Refresh();
                if (filterType == ExtensionType.Unknown || filterType == ExtensionType.Extension)
                    WriteObject(installation.InstalledExtensions, true);
                if (filterType == ExtensionType.Unknown || filterType == ExtensionType.Skin)
                    WriteObject(installation.InstalledSkins, true);
            }
        }
    }

}
