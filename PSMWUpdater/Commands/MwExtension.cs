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
    /// <para type="synopsis">Gets a list of extensions either from a local MediaWiki installation or from WMF-registered MediaWiki extensions.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, NounsCommon.MwExtension, DefaultParameterSetName = "Remote")]
    [OutputType(typeof(LocalExtensionInfo), ParameterSetName = new[] { "Local" })]
    [OutputType(typeof(ExtensionName), typeof(LocalExtensionInfo), ParameterSetName = new[] { "LocalName" })]
    [OutputType(typeof(ExtensionInfo), ParameterSetName = new[] { "Remote" })]
    [OutputType(typeof(ExtensionName), typeof(LocalExtensionInfo), ParameterSetName = new[] { "RemoteName" })]
    public class GetMwExtensionCommand : PSAsyncCmdlet
    {

        /// <summary>
        /// <para type="description">If specified, gets all the extensions from the local MediaWiki installation in the specified path,
        /// instead of retrieving known extensions from server.</para>
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Local")]
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "LocalName")]
        public string InstallationPath { get; set; }

        /// <summary>
        /// <para type="description">Return bare <see cref="ExtensionName"/> instead of <see cref="LocalExtensionInfo"/>-derived class.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "LocalName")]
        [Parameter(Mandatory = true, ParameterSetName = "RemoteName")]
        public SwitchParameter BareName { get; set; }

        /// <summary>
        /// <para type="description">Only list extensions of this type.</para>
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
                var progress = new ProgressRecord(1, "Get MediaWiki extension inventory", "Connecting to server.");
                WriteProgress(progress);
                var site = await AmbientServices.GetExtensionProviderSiteAsync();
                progress.StatusDescription = "Fetching extensions.";
                progress.PercentComplete = 30;
                WriteProgress(progress);
                var names = await site.GetKnownExtensionsAsync(cancellationToken);
                progress.StatusDescription = "Processing response.";
                progress.PercentComplete = 60;
                WriteProgress(progress);
                var processedCount = 0;
                foreach (var name in names)
                {
                    processedCount++;
                    if (filterType != ExtensionType.Unknown && name.Type != filterType)
                        continue;
                    WriteObject(SelectResult(new ExtensionInfo(name)));
                    progress.PercentComplete = 60 + (int)(40.0 * processedCount / names.Count);
                    progress.StatusDescription = $"Outputting response: {processedCount}/{names.Count}";
                    WriteProgress(progress);
                }
                progress.StatusDescription = "Completed.";
                progress.PercentComplete = 100;
                WriteProgress(progress);
            }
            else
            {
                var resolvedPath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(InstallationPath);
                MediaWikiInstallation.AssertMwRootFolder(resolvedPath, nameof(InstallationPath));
                var installation = new MediaWikiInstallation(resolvedPath);
                if (filterType == ExtensionType.Unknown || filterType == ExtensionType.Extension)
                {
                    WriteObject(installation.GetExtensions(this).Select(SelectResult), true);
                }
                if (filterType == ExtensionType.Unknown || filterType == ExtensionType.Skin)
                {
                    WriteObject(installation.GetSkins(this).Select(SelectResult), true);
                }
            }
        }
    }

}
