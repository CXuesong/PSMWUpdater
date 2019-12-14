using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WikiClientLibrary.Client;
using WikiClientLibrary.Infrastructures;
using WikiClientLibrary.Sites;

namespace PSMWUpdater
{

    [JsonObject]
    public class ExtensionReposInfo
    {

        [JsonProperty]
        public IList<string> Extensions { get; set; }

        [JsonProperty]
        public IList<string> Skins { get; set; }

    }

    [JsonObject]
    public class ExtensionBranchesInfo
    {

        // Version spec -- URL

        [JsonProperty]
        public IList<IDictionary<string, string>> Extensions { get; set; }

        [JsonProperty]
        public IList<IDictionary<string, string>> Skins { get; set; }

    }

    public static class WikiSiteExtensions
    {

        private static readonly JsonSerializer mwJsonSerializer = MediaWikiHelper.CreateWikiJsonSerializer();

        public static async Task<ExtensionReposInfo> GetKnownExtensionsAsync(this WikiSite site, CancellationToken cancellationToken = default)
        {
            if (site == null) throw new ArgumentNullException(nameof(site));
            var response = await site.InvokeMediaWikiApiAsync(
                new MediaWikiFormRequestMessage(new { action = "query", list = "extdistrepos" }),
                cancellationToken);
            var node = response["query"]["extdistrepos"];
            return node.ToObject<ExtensionReposInfo>(mwJsonSerializer);
        }

        public static async Task

    }

}
