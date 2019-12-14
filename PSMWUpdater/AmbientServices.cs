using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WikiClientLibrary.Client;
using WikiClientLibrary.Sites;

namespace PSMWUpdater
{
    internal static class AmbientServices
    {

        public static WikiClient WikiClient { get; } = new WikiClient { ClientUserAgent = "PSMWUpdater/1.0" };

        private static (string, WikiSite) _extensionProviderSiteCache;

        public static Task<WikiSite> GetExtensionProviderSiteAsync()
        {
            return GetExtensionProviderSiteAsync("https://www.mediawiki.org/w/api.php");
        }

        public static async Task<WikiSite> GetExtensionProviderSiteAsync(string siteEndpointUrl)
        {
            if (siteEndpointUrl == null) throw new ArgumentNullException(nameof(siteEndpointUrl));
            var (url, site) = _extensionProviderSiteCache;
            if (siteEndpointUrl != url)
            {
                site = new WikiSite(WikiClient, siteEndpointUrl);
                await site.Initialization;
                _extensionProviderSiteCache = (url, site);
            }
            return site;
        }

    }
}
