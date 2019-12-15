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
    [Cmdlet(VerbsCommon.Get, NounsCommon.MwExtension, DefaultParameterSetName = "Remote")]
    [OutputType(typeof(LocalExtensionInfo), ParameterSetName = new[] { "Local" })]
    [OutputType(typeof(ExtensionName), typeof(LocalExtensionInfo), ParameterSetName = new[] { "LocalName" })]
    [OutputType(typeof(ExtensionInfo), ParameterSetName = new[] { "Remote" })]
    [OutputType(typeof(ExtensionName), typeof(LocalExtensionInfo), ParameterSetName = new[] { "RemoteName" })]
    public class GetMwExtensionCommand : PSAsyncCmdlet
    {

        /// <summary>
        /// If specified, gets all the extensions from the local MediaWiki installation in the specified path,
        /// instead of retrieving known extensions from server.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Local")]
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "LocalName")]
        public string InstallationPath { get; set; }

        /// <summary>
        /// When retrieving extensions from local installation, excludes empty folders.
        /// </summary>
        [Parameter(ParameterSetName = "Local")]
        [Parameter(ParameterSetName = "LocalName")]
        public SwitchParameter NoEmpty { get; set; }

        /// <summary>
        /// Return bare <see cref="ExtensionName"/> instead of <see cref="LocalExtensionInfo"/>-derived class.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "LocalName")]
        [Parameter(Mandatory = true, ParameterSetName = "RemoteName")]
        public SwitchParameter BareName { get; set; }

        /// <summary>
        /// Only list extensions of this type.
        /// </summary>
        [Parameter(ParameterSetName = "Local")]
        [Parameter(ParameterSetName = "Remote")]
        [Parameter(ParameterSetName = "LocalName")]
        [Parameter(ParameterSetName = "RemoteName")]
        [ValidateSet("All", nameof(ExtensionType.Extension), nameof(ExtensionType.Skin), IgnoreCase = true)]
        public string Type { get; set; }

        /// <inheritdoc />
        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            var filterType = Type == null || "All".Equals(Type, StringComparison.OrdinalIgnoreCase)
                ? ExtensionType.Unknown
                : (ExtensionType)Enum.Parse(typeof(ExtensionType), Type, true);

            object SelectResult(ExtensionInfo e)
            {
                if (BareName)
                    return e.Name;
                return e;
            }
            if (InstallationPath == null)
            {
                var site = await AmbientServices.GetExtensionProviderSiteAsync();
                var names = await site.GetKnownExtensionsAsync(cancellationToken);
                foreach (var name in names)
                {
                    if (filterType != ExtensionType.Unknown && name.Type != filterType)
                        continue;
                    WriteObject(SelectResult(new ExtensionInfo(name)));
                }
            }
            else
            {
                var resolvedPath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(InstallationPath);
                if (!MediaWikiInstallation.CheckMwRootFolder(resolvedPath))
                    throw new ArgumentException("Specified path is not a valid MediaWiki installation.", nameof(InstallationPath));
                var installation = new MediaWikiInstallation(resolvedPath);
                installation.Refresh();
                bool FilterExtension(LocalExtensionInfo e)
                {
                    if (NoEmpty && e.IsEmpty) return false;
                    return true;
                }
                if (filterType == ExtensionType.Unknown || filterType == ExtensionType.Extension)
                    WriteObject(installation.InstalledExtensions.Where(FilterExtension).Select(SelectResult), true);
                if (filterType == ExtensionType.Unknown || filterType == ExtensionType.Skin)
                    WriteObject(installation.InstalledSkins.Where(FilterExtension).Select(SelectResult), true);
            }
        }
    }

}
