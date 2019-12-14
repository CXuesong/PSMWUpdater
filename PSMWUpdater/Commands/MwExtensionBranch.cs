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
    public class GetMwExtensionBranch : AsyncCmdlet
    {

        /// <summary>
        /// One or more names of the extensions to fetch information.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        public ExtensionName[] Name { get; set; }

        /// <summary>
        /// One or more version branches for the extension (e.g. "REL1_32", or "master").
        /// </summary>
        public string[] Branch { get; set; }

        /// <inheritdoc />
        protected override async Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            if (Name.Length == 0) return;
            var batch = new List<ExtensionName>();
            var branchSet = Branch == null || Branch.Length == 0 ? null : new HashSet<string>(Branch, StringComparer.OrdinalIgnoreCase);
            var site = await AmbientServices.GetExtensionProviderSiteAsync();
            async Task ProcessBatchAsync()
            {
                IEnumerable<ExtensionBranchInfo> branches = await site.GetExtensionBranchesAsync(batch, cancellationToken);
                if (branchSet != null)
                    branches = branches.Where(b => branchSet.Contains(b.BranchName));
                WriteObject(branches, true);
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
