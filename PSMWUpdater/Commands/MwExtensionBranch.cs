using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using PSMWUpdater.Infrastructures;
using WikiClientLibrary.Pages;

namespace PSMWUpdater.Commands
{

    /// <summary>
    /// <para type="synopsis">
    /// Gets the branch information about one or more MediaWiki extensions, including their download URLs.
    /// </para>
    /// </summary>
    /// <example>
    /// <para>Ask for the latest `REL` branch.</para>
    /// <para></para>
    /// <code>
    /// PS&gt; Get-MwExtensionBranch -Name Vector, Echo
    /// ExtensionName  BranchName Url
    /// -------------  ---------- ---
    /// Extension:Echo REL1_33    https://extdist.wmflabs.org/dist/extensions/Echo-REL1_33-f106596.tar.gz
    /// Skin:Vector    REL1_33    https://extdist.wmflabs.org/dist/skins/Vector-REL1_33-878c1e8.tar.gz
    /// </code>
    /// </example>
    /// <example>
    /// <para>Ask for a specific branch instead of latest `REL` branch.</para>
    /// <para></para>
    /// <code>
    /// PS&gt; Get-MwExtensionBranch -Name Vector, Echo
    /// ExtensionName  BranchName Url
    /// -------------  ---------- ---
    /// Extension:Echo REL1_33    https://extdist.wmflabs.org/dist/extensions/Echo-REL1_33-f106596.tar.gz
    /// Skin:Vector    REL1_33    https://extdist.wmflabs.org/dist/skins/Vector-REL1_33-878c1e8.tar.gz
    /// </code>
    /// </example>
    /// <example>
    /// <para>Ask for all the known branches.</para>
    /// <para></para>
    /// <code>
    /// PS&gt; Get-MwExtensionBranch -Name Vector, Echo -AllBranches
    /// 
    /// ExtensionName  BranchName Url
    /// -------------  ---------- ---
    /// Extension:Echo REL1_31    https://extdist.wmflabs.org/dist/extensions/Echo-REL1_31-b56ec9b.tar.gz
    /// Extension:Echo REL1_32    https://extdist.wmflabs.org/dist/extensions/Echo-REL1_32-335389f.tar.gz
    /// Extension:Echo REL1_33    https://extdist.wmflabs.org/dist/extensions/Echo-REL1_33-f106596.tar.gz
    /// Extension:Echo master     https://extdist.wmflabs.org/dist/extensions/Echo-master-4c991af.tar.gz
    /// Extension:Echo source     https://gerrit.wikimedia.org/r/mediawiki/extensions/Echo.git
    /// Skin:Vector    REL1_31    https://extdist.wmflabs.org/dist/skins/Vector-REL1_31-f0327dc.tar.gz
    /// Skin:Vector    REL1_32    https://extdist.wmflabs.org/dist/skins/Vector-REL1_32-d3ed21a.tar.gz
    /// Skin:Vector    REL1_33    https://extdist.wmflabs.org/dist/skins/Vector-REL1_33-878c1e8.tar.gz
    /// Skin:Vector    master     https://extdist.wmflabs.org/dist/skins/Vector-master-bf365aa.tar.gz
    /// Skin:Vector    source     https://gerrit.wikimedia.org/r/mediawiki/skins/Vector.git
    /// </code>
    /// </example>
    [Cmdlet(VerbsCommon.Get, NounsCommon.MwExtensionBranch)]
    [OutputType(typeof(ExtensionBranchInfo))]
    public class GetMwExtensionBranchCommand : PSAsyncCmdlet
    {

        /// <summary>
        /// <para type="description">One or more names of the extensions to fetch information.</para>
        /// <para type="description">If there is ambiguity on whether the extension is a skin, prefix the extension name with `Extension:` or `Skin:`.</para>
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public ExtensionName[] Name { get; set; }

        /// <summary>
        /// <para type="description">One or more version branches for the extension (e.g. "REL1_32", or "master").</para>
        /// <para type="description">If `AllBranches` is not specified, the first matching branch is fetched.
        /// Otherwise, all the matching branches are fetched.</para>
        /// <para type="description">If `AllBranches` is not specified and this parameter is neglected,
        /// the cmdlet will return the latest REL branch.</para>
        /// </summary>
        [Parameter]
        public string[] Branch { get; set; }

        /// <summary>
        /// <para type="description">Return all the matching branches instead of only the first matching branch.</para>
        /// </summary>
        [Parameter]
        public SwitchParameter AllBranches { get; set; }

        /// <summary>
        /// <para type="description">Whether to suppress the attempt to look for extension redirects
        /// (e.g. SyntaxHighlight --> SyntaxHighlight GeSHi).</para>
        /// </summary>
        [Parameter]
        public SwitchParameter SuppressRedirect { get; set; }

        private const string promptNoMatchingBranch = "No matching branch for extension \"{0}\". Available branches: {1}.";
        private const string promptNoMatchingRelBranch = "No matching REL branch for extension \"{0}\". Available branches: {1}.";
        private const string promptNoBranch = "No branch found for extension \"{0}\".";

        private HashSet<ExtensionName> batch;
        private int currentBatchCounter = 0;
        private int inputCounter;
        private int processedCounter;
        private ProgressRecord progress;

        /// <inheritdoc />
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            batch = new HashSet<ExtensionName>();
            inputCounter = processedCounter = 0;
            progress = new ProgressRecord(1, "Get MediaWiki extension branch information", "Waiting input.");
            WriteProgress(progress);
        }

        private void UpdateProgress()
        {
            if (inputCounter > 0)
            {
                progress.StatusDescription = $"Processed {processedCounter}/{inputCounter}";
                progress.PercentComplete = (int)(processedCounter * 100.0 / inputCounter);
                WriteProgress(progress);
            }
        }

        private static readonly Regex mwExtensionDownloadNameMatcher = new Regex(@"{{WikimediaDownload\|(.+?)}}", RegexOptions.IgnoreCase);
        private static readonly Regex mwSkinDownloadNameMatcher = new Regex(@"{{WikimediaDownloadSkin\|(.+?)}}", RegexOptions.IgnoreCase);

        private async Task ProcessBatchAsync(CancellationToken cancellationToken)
        {
            var batchProgress = new ProgressRecord(2, "MediaWiki connection", "Connecting to server.");
            WriteProgress(batchProgress);
            var site = await AmbientServices.GetExtensionProviderSiteAsync();
            batchProgress.StatusDescription = "Fetching extension information.";
            batchProgress.PercentComplete = 30;
            WriteProgress(batchProgress);
            var branchSet = Branch == null ? null : new HashSet<string>(Branch, StringComparer.OrdinalIgnoreCase);
            var branches = await site.GetExtensionBranchesAsync(batch, cancellationToken);
            batchProgress.StatusDescription = "Processing response.";
            batchProgress.PercentComplete = 60;
            WriteProgress(batchProgress);
            ProcessResult();

            void ProcessResult()
            {
                foreach (var p in branches)
                {
                    var extensionBranches = p.Value;
                    if (AllBranches)
                    {
                        if (branchSet != null)
                            extensionBranches = p.Value.Where(b => branchSet.Contains(b.BranchName)).ToList();
                        if (extensionBranches.Count > 0)
                            WriteObject(extensionBranches, true);
                        else
                            WriteWarning(string.Format(promptNoMatchingBranch, p.Key, string.Join(", ", p.Value.Select(b => b.BranchName))));
                    }
                    else if (Branch != null)
                    {
                        var matchingBranch = Branch.Select(bn => extensionBranches.FirstOrDefault(b1 => b1.BranchName == bn)).FirstOrDefault(b => b != null);
                        if (matchingBranch != null)
                            WriteObject(matchingBranch);
                        else
                            WriteWarning(string.Format(promptNoMatchingBranch, p.Key, string.Join(", ", p.Value.Select(b => b.BranchName))));
                    }
                    else
                    {
                        // Find latest REL branch.
                        var latestRel = extensionBranches
                            .Where(b => b.BranchName.StartsWith("REL", StringComparison.Ordinal))
                            .OrderByDescending(b => b.BranchName)
                            .FirstOrDefault();
                        if (latestRel != null)
                            WriteObject(latestRel);
                        else
                            WriteWarning(string.Format(promptNoMatchingRelBranch, p.Key, string.Join(", ", p.Value.Select(b => b.BranchName))));
                    }
                    batch.Remove(p.Key);
                    batch.Remove(new ExtensionName(p.Key.Name, ExtensionType.Unknown));
                }
            }

            if (batch.Count > 0 && !SuppressRedirect)
            {
                batchProgress.StatusDescription = $"Look for extension redirects ({batch.Count}).";
                batchProgress.PercentComplete = 60;
                WriteProgress(batchProgress);
                var names = new List<ExtensionName>();
                foreach (var name in batch)
                {
                    if (name.Type == ExtensionType.Unknown)
                    {
                        names.Add(new ExtensionName(name.Name, ExtensionType.Extension));
                        names.Add(new ExtensionName(name.Name, ExtensionType.Skin));
                    }
                    else
                    {
                        names.Add(name);
                    }
                }
                var pages = names.Select(n => (OriginalName: n, Page: new WikiPage(site, n.ToString()))).ToList();
                await pages.Select(t => t.Page).RefreshAsync(PageQueryOptions.FetchContent | PageQueryOptions.ResolveRedirects, cancellationToken);
                batchProgress.StatusDescription = $"Look for extension redirects ({batch.Count}).";
                batchProgress.PercentComplete = 70;
                WriteProgress(batchProgress);
                var redirectedNames = new HashSet<ExtensionName>();
                foreach (var (originalName, page) in pages)
                {
                    if (!page.Exists)
                    {
                        WriteVerbose($"Failed to find extension redirect for {originalName}.");
                        continue;
                    }
                    var match = mwExtensionDownloadNameMatcher.Match(page.Content);
                    if (match.Success)
                    {
                        var name = new ExtensionName(match.Groups[1].Value.Trim(), ExtensionType.Extension);
                        WriteWarning($"Redirected {originalName} to {name}.");
                        batch.Remove(originalName);
                        redirectedNames.Add(name);
                    }
                    match = mwSkinDownloadNameMatcher.Match(page.Content);
                    if (match.Success)
                    {
                        var name = new ExtensionName(match.Groups[1].Value.Trim(), ExtensionType.Skin);
                        WriteWarning($"Redirected {originalName} to {name}.");
                        batch.Remove(originalName);
                        redirectedNames.Add(name);
                    }
                }
                if (redirectedNames.Count > 0)
                {
                    batchProgress.StatusDescription = $"Fetching {redirectedNames.Count} extensions.";
                    batchProgress.PercentComplete = 80;
                    WriteProgress(batchProgress);
                    branches = await site.GetExtensionBranchesAsync(redirectedNames, cancellationToken);
                    foreach (var n in redirectedNames)
                        batch.Add(n);
                    batchProgress.StatusDescription = "Processing response.";
                    batchProgress.PercentComplete = 90;
                    WriteProgress(batchProgress);
                    ProcessResult();
                }
            }
            foreach (var name in batch)
            {
                WriteWarning(string.Format(promptNoBranch, name));
            }
            batchProgress.PercentComplete = 100;
            WriteProgress(batchProgress);
        }

        /// <inheritdoc />
        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            if (Name == null || Name.Length == 0 || Branch != null && Branch.Length == 0) return;
            inputCounter += Name.Length;
            UpdateProgress();
            foreach (var n in Name)
            {
                batch.Add(n);
                currentBatchCounter++;
                if (batch.Count >= 30)
                {
                    await ProcessBatchAsync(cancellationToken);
                    batch.Clear();
                    processedCounter += currentBatchCounter;
                    currentBatchCounter = 0;
                    UpdateProgress();
                }
            }
        }

        /// <inheritdoc />
        protected override async Task EndProcessingAsync(CancellationToken cancellationToken)
        {
            processedCounter += currentBatchCounter;
            if (batch.Count > 0)
                await ProcessBatchAsync(cancellationToken);
            UpdateProgress();
            batch = null;
        }

    }

}
