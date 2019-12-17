using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace PSMWUpdater
{

    internal class MediaWikiInstallation
    {

        public MediaWikiInstallation(string rootPath)
        {
            RootPath = Path.GetFullPath(rootPath);
        }

        public string RootPath { get; }

        public IReadOnlyList<LocalExtensionInfo> InstalledExtensions { get; private set; }

        public IReadOnlyList<LocalExtensionInfo> InstalledSkins { get; private set; }

        private static readonly Regex mwExtensionPageUrl = new Regex(@"https?://www\.mediawiki\.org/wiki/(Extension:|Skin:)(?<N>.+)", RegexOptions.IgnoreCase);

        private static LocalExtensionInfo SelectExtensionInfo(Cmdlet owner, ExtensionType type, string path)
        {
            var manifestName = type == ExtensionType.Extension ? "extension.json" : "skin.json";
            var manifestPath = Path.Combine(path, manifestName);
            if (!File.Exists(manifestPath))
            {
                owner.WriteVerbose($"{path}: Cannot find {manifestName}.");
                return null;
            }
            var extensionManifest = JObject.Parse(File.ReadAllText(manifestPath));

            // Some extensions, like CLDR, uses different name as folder name (Cldr)
            var url = (string)extensionManifest["url"];
            var match = mwExtensionPageUrl.Match(url);
            string name;
            if (match.Success)
            {
                name = match.Groups["N"].Value.Replace('_', ' ');
            }
            else
            {
                name = (string)extensionManifest["name"];
            }
            return new LocalExtensionInfo(new ExtensionName(string.IsNullOrEmpty(name) ? Path.GetFileName(path) : name, type), path);
        }

        public void RefreshExtensions(Cmdlet owner)
        {
            var dirs = Directory.GetDirectories(Path.Combine(RootPath, "extensions"));
            InstalledExtensions = dirs.Select((path, i) =>
                {
                    owner.WriteProgress(new ProgressRecord(10, "Enumerate local MediaWiki extensions", $"Processed folders: {i + 1}/{dirs.Length}: {Path.GetFileName(path)}")
                    {
                        PercentComplete = (int)((i + 1) * 100.0 / dirs.Length)
                    });
                    return SelectExtensionInfo(owner, ExtensionType.Extension, path);
                })
                .Where(i => i != null)
                .ToList();
        }

        public void RefreshSkins(Cmdlet owner)
        {
            var dirs = Directory.GetDirectories(Path.Combine(RootPath, "skins"));
            InstalledSkins = dirs.Select((path, i) =>
                {
                    owner.WriteProgress(new ProgressRecord(10, "Enumerate local MediaWiki skins", $"Processed folders: {i + 1}/{dirs.Length}: {Path.GetFileName(path)}")
                    {
                        PercentComplete = (int)((i + 1) * 100.0 / dirs.Length)
                    });
                    return SelectExtensionInfo(owner, ExtensionType.Skin, path);
                })
                .Where(i => i != null)
                .ToList();
        }

        public static bool CheckMwRootFolder(string path)
        {
            if (!File.Exists(Path.Combine(path, "index.php")))
                return false;
            if (!Directory.Exists(Path.Combine(path, "extensions")))
                return false;
            if (!Directory.Exists(Path.Combine(path, "skins")))
                return false;
            return true;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return RootPath;
        }
    }

}
