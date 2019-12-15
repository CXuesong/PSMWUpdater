using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PSMWUpdater.Infrastructures;

namespace PSMWUpdater.Commands
{

    [Cmdlet(VerbsCommon.Get, NounsCommon.MwExtensionBranch)]
    [OutputType(typeof(ExtensionBranchInfo))]
    public class GetMwExtensionBranchCommand : PSAsyncCmdlet
    {

        /// <summary>
        /// One or more names of the extensions to fetch information.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        public ExtensionName[] Name { get; set; }

        /// <summary>
        /// One or more version branches for the extension (e.g. "REL1_32", or "master").
        /// If `AllBranches` is not specified, the first matching branch is fetched.
        /// Otherwise, all the matching branches are fetched.
        /// If `AllBranches` is not specified and this parameter is neglected,
        /// the cmdlet will return the latest REL branch.
        /// </summary>
        [Parameter]
        public string[] Branch { get; set; }

        /// <summary>
        /// Return all the matching branches instead of only the first matching branch.
        /// </summary>
        [Parameter]
        public SwitchParameter AllBranches { get; set; }

        private const string promptNoMatchingBranch = "No matching branch for extension \"{0}\". Available branches: {1}.";
        private const string promptNoMatchingRelBranch = "No matching REL branch for extension \"{0}\". Available branches: {1}.";
        private const string promptNoBranch = "No branch found for extension \"{0}\".";

        /// <inheritdoc />
        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            if (Name == null || Name.Length == 0 || Branch != null && Branch.Length == 0) return;
            var batch = new HashSet<ExtensionName>();
            var branchSet = Branch == null ? null : new HashSet<string>(Branch, StringComparer.OrdinalIgnoreCase);
            var site = await AmbientServices.GetExtensionProviderSiteAsync();
            async Task ProcessBatchAsync()
            {
                var branches = await site.GetExtensionBranchesAsync(batch, cancellationToken);
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
                foreach (var name in batch)
                {
                    WriteWarning(string.Format(promptNoBranch, name));
                }
            }
            foreach (var n in Name)
            {
                batch.Add(n);
                if (batch.Count >= 30)
                {
                    await ProcessBatchAsync();
                    batch.Clear();
                }
            }
            if (batch.Count > 0)
                await ProcessBatchAsync();
        }
    }

}
