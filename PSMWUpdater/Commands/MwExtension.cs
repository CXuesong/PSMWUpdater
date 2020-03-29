using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using PSMWUpdater.Infrastructures;

namespace PSMWUpdater.Commands
{

    /// <summary>
    /// <para type="synopsis">
    /// Gets a list of extensions either from a local MediaWiki installation, LocalSettings.php,
    /// or WMF MediaWiki extension registry.
    /// </para>
    /// </summary>
    /// <example>
    /// <para>Gets extension name list from WMF MediaWiki registry.</para>
    /// <code>
    /// PS> Get-MwExtension -BareName
    /// Name                                 Type
    /// ----                                 ----
    /// 3D                              Extension
    /// AJAXPoll                        Extension
    /// AbsenteeLandlord                Extension
    /// AbuseFilter                     Extension
    /// AbuseFilterBypass               Extension
    /// AccessControl                   Extension
    /// AccountInfo                     Extension
    /// ...
    /// </code>
    /// </example>
    /// <example>
    /// <para>Gets installed extensions from a MediaWiki installation.</para>
    /// <code>
    /// PS> Get-MwExtension -LocalSettingsPath X:\mediawiki-1.33.0
    /// LineNumber LocalPath                                                            Name
    /// ---------- ---------                                                            ----
    ///          5 X:\mediawiki-1.33.0\skins\CologneBlue                     Skin:CologneBlue
    ///          6 X:\mediawiki-1.33.0\skins\Modern                          Skin:Modern
    ///          7 X:\mediawiki-1.33.0\skins\MonoBook                        Skin:MonoBook
    ///          8 X:\mediawiki-1.33.0\skins\Vector                          Skin:Vector
    ///          9 X:\mediawiki-1.33.0\skins\Timeless                        Skin:Timeless
    ///         22 X:\mediawiki-1.33.0\extensions\Babel                      Extension:Babel
    ///         23 X:\mediawiki-1.33.0\extensions\Cite                       Extension:Cite
    /// ...
    /// </code>
    /// </example>
    /// <example>
    /// <para>Gets installed extensions from a local MediaWiki installation.</para>
    /// <code>
    /// PS> Get-MwExtension -InstallationPath X:\mediawiki-1.33.0
    /// Name                           Branch   Version  Revision RevisionTime              LocalPath
    /// ----                           ------   -------  -------- ------------              ---------
    /// Extension:CategoryTree                                                              X:\mediawiki-1.33.0\extensions\CategoryTree
    /// Extension:Cite                          1.0.0                                       X:\mediawiki-1.33.0\extensions\Cite
    /// Extension:CiteThisPage                                                              X:\mediawiki-1.33.0\extensions\CiteThisPage
    /// Extension:CodeEditor                                                                X:\mediawiki-1.33.0\extensions\CodeEditor
    /// Extension:InputBox                      0.3.0                                       X:\mediawiki-1.33.0\extensions\InputBox
    /// Extension:intersection         REL1_33  1.7.0    05edc37  6/17/2019 7:24:11 PM      X:\mediawiki-1.33.0\extensions\intersection
    /// Extension:Interwiki                     3.1 201…                                    X:\mediawiki-1.33.0\extensions\Interwiki
    /// Extension:LocalisationUpdate            1.4.0                                       X:\mediawiki-1.33.0\extensions\LocalisationUpdate
    /// ...
    /// </code>
    /// </example>
    [Cmdlet(VerbsCommon.Get, NounsCommon.MwExtension, DefaultParameterSetName = "Remote")]
    [OutputType(typeof(LocalExtensionInfo), ParameterSetName = new[] { "Local" })]
    [OutputType(typeof(ExtensionName), typeof(LocalExtensionInfo), ParameterSetName = new[] { "LocalName" })]
    [OutputType(typeof(LocalSettingsExtensionInfo), ParameterSetName = new[] { "LocalSettings" })]
    [OutputType(typeof(ExtensionName), typeof(LocalSettingsExtensionInfo), ParameterSetName = new[] { "LocalSettingsName" })]
    [OutputType(typeof(ExtensionInfo), ParameterSetName = new[] { "Remote" })]
    [OutputType(typeof(ExtensionName), typeof(LocalExtensionInfo), ParameterSetName = new[] { "RemoteName" })]
    public class GetMwExtensionCommand : PSAsyncCmdlet
    {

        /// <summary>
        /// <para type="description">Path of the local MediaWiki installation.</para>
        /// <para type="description">If specified, gets all the installed extensions from a local MediaWiki installation.</para>
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Local")]
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "LocalName")]
        public string InstallationPath { get; set; }

        /// <summary>
        /// <para type="description">Path of the LocalSettings.php file. If a folder is specified, it will be appended LocalSettings.php automatically.</para>
        /// <para type="description">If specified, parses the file and outputs all the extensions and/or skins referenced in the file.</para>
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "LocalSettings")]
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "LocalSettingsName")]
        public string LocalSettingsPath { get; set; }

        /// <summary>
        /// <para type="description">Return bare <see cref="ExtensionName"/> instead of <see cref="LocalExtensionInfo"/>-derived class.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "LocalName")]
        [Parameter(Mandatory = true, ParameterSetName = "RemoteName")]
        [Parameter(Mandatory = true, ParameterSetName = "LocalSettingsName")]
        public SwitchParameter BareName { get; set; }

        /// <summary>
        /// <para type="description">Only list extensions of this type.</para>
        /// </summary>
        [Parameter(ParameterSetName = "Local")]
        [Parameter(ParameterSetName = "Remote")]
        [Parameter(ParameterSetName = "LocalSettings")]
        [Parameter(ParameterSetName = "LocalName")]
        [Parameter(ParameterSetName = "RemoteName")]
        [Parameter(ParameterSetName = "LocalSettingsName")]
        [ValidateSet("All", nameof(ExtensionType.Extension), nameof(ExtensionType.Skin), IgnoreCase = true)]
        public string Type { get; set; }

        /// <inheritdoc />
        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            var filterType = Type == null || "All".Equals(Type, StringComparison.OrdinalIgnoreCase)
                ? ExtensionType.Unknown
                : (ExtensionType) Enum.Parse(typeof(ExtensionType), Type, true);

            object SelectResult(ExtensionInfo e)
            {
                if (BareName)
                    return e.Name;
                return e;
            }

            switch (ParameterSetName)
            {
                case "Remote":
                case "RemoteName":
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
                        progress.PercentComplete = 60 + (int) (40.0 * processedCount / names.Count);
                        progress.StatusDescription = $"Outputting response: {processedCount}/{names.Count}";
                        WriteProgress(progress);
                    }
                    progress.StatusDescription = "Completed.";
                    progress.PercentComplete = 100;
                    WriteProgress(progress);
                }
                    break;
                case "Local":
                case "LocalName":
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
                    break;
                case "LocalSettings":
                case "LocalSettingsName":
                {
                    var resolvedPath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(LocalSettingsPath);
                    if (Directory.Exists(resolvedPath))
                        resolvedPath = Path.Combine(resolvedPath, "LocalSettings.php");
                    WriteObject(MediaWikiInstallation.GetLocalSettingsExtensions(this, resolvedPath)
                        .Where(e => filterType == ExtensionType.Unknown || filterType == e.Name.Type)
                        .Select(SelectResult), true);
                }
                    break;
            }
        }
    }

}
