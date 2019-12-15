using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WikiClientLibrary.Client;
using WikiClientLibrary.Infrastructures;
using WikiClientLibrary.Sites;

namespace PSMWUpdater
{

    internal static class WikiSiteExtensions
    {

        private static readonly JsonSerializer mwJsonSerializer = MediaWikiHelper.CreateWikiJsonSerializer();

        public static async Task<IList<ExtensionName>> GetKnownExtensionsAsync(this WikiSite site, CancellationToken cancellationToken = default)
        {
            if (site == null) throw new ArgumentNullException(nameof(site));
            var response = await site.InvokeMediaWikiApiAsync(
                new MediaWikiFormRequestMessage(new
                {
                    action = "query",
                    list = "extdistrepos"
                }),
                cancellationToken);
            var node = response["query"]["extdistrepos"];
            var extensions = node["extensions"]?.ToObject<IList<string>>() ?? Enumerable.Empty<string>();
            var skins = node["skins"]?.ToObject<IList<string>>() ?? Enumerable.Empty<string>();
            return extensions.Select(e => new ExtensionName(e, ExtensionType.Extension))
                .Concat(skins.Select(s => new ExtensionName(s, ExtensionType.Skin)))
                .ToList();
        }

        private static readonly Dictionary<string, IDictionary<string, string>> emptyDict2 = new Dictionary<string, IDictionary<string, string>>();

        public static async Task<IDictionary<ExtensionName, IList<ExtensionBranchInfo>>> GetExtensionBranchesAsync(this WikiSite site, IReadOnlyCollection<ExtensionName> names,
            CancellationToken cancellationToken = default)
        {
            if (site == null) throw new ArgumentNullException(nameof(site));
            var response = await site.InvokeMediaWikiApiAsync(
                new MediaWikiFormRequestMessage(new
                {
                    action = "query",
                    list = "extdistbranches",
                    edbexts = MediaWikiHelper.JoinValues(names
                        .Where(n => n.Type == ExtensionType.Unknown || n.Type == ExtensionType.Extension)
                        .Select(n => n.Name)),
                    edbskins = MediaWikiHelper.JoinValues(names
                        .Where(n => n.Type == ExtensionType.Unknown || n.Type == ExtensionType.Skin)
                        .Select(n => n.Name)),
                }),
                cancellationToken);
            var node = response["query"]["extdistbranches"];
            var extensions = node["extensions"]?.ToObject<IDictionary<string, IDictionary<string, string>>>() ?? emptyDict2;
            var skins = node["skins"]?.ToObject<IDictionary<string, IDictionary<string, string>>>() ?? emptyDict2;
            return extensions.Select(e =>
                    e.Value.Select(b => new ExtensionBranchInfo(new ExtensionName(e.Key, ExtensionType.Extension), b.Key, b.Value)).ToList())
                .Concat(skins.Select(s => s.Value.Select(b => new ExtensionBranchInfo(new ExtensionName(s.Key, ExtensionType.Skin), b.Key, b.Value)).ToList()))
                .ToDictionary(e => e.First().ExtensionName, e => (IList<ExtensionBranchInfo>)e);
        }

    }

}
