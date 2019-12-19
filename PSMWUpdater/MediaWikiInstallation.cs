using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
            owner.WriteVerbose($"{path}: Load {manifestName}.");
            var extensionManifest = JObject.Parse(File.ReadAllText(manifestPath));
            // Some extensions, like CLDR, uses different name as folder name (Cldr)
            var url = (string)extensionManifest["url"];
            var version = (string)extensionManifest["version"];
            string branch = null;
            string revision = null;
            var match = mwExtensionPageUrl.Match(url);
            var revisionTime = DateTime.MinValue;
            string name;
            if (match.Success)
            {
                name = match.Groups["N"].Value.Replace('_', ' ');
            }
            else
            {
                name = (string)extensionManifest["name"];
            }
            const string versionFileName = "version";
            var versionFilePath = Path.Combine(path, versionFileName);
            if (File.Exists(versionFilePath))
            {
                owner.WriteVerbose($"{path}: Load {versionFileName} file.");
                var content = File.ReadLines(versionFilePath).Where(l => !string.IsNullOrWhiteSpace(l)).Take(3).ToList();
                if (content.Count > 0)
                {
                    var fields = content[0].Split(new[] { ':' }, 2);
                    // This should be most accurate.
                    name = fields[0].Trim();
                    if (fields.Length > 1)
                        branch = fields[1].Trim();
                }
                if (content.Count > 1)
                    revisionTime = DateTime.Parse(content[1], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                if (content.Count > 2)
                    revision = content[2].Trim();
            }
            else
            {
                owner.WriteVerbose($"{path}: Cannot find {versionFileName} file.");
            }
            return new LocalExtensionInfo(new ExtensionName(string.IsNullOrEmpty(name) ? Path.GetFileName(path) : name, type), path, branch, version, revision, revisionTime);
        }

        public IList<LocalExtensionInfo> GetExtensions(Cmdlet owner)
        {
            var dirs = Directory.GetDirectories(Path.Combine(RootPath, "extensions"));
            return dirs.Select((path, i) =>
                {
                    owner.WriteProgress(new ProgressRecord(10, "Enumerate local MediaWiki extensions",
                        $"Processed folders: {i + 1}/{dirs.Length}: {Path.GetFileName(path)}")
                    {
                        PercentComplete = (int)((i + 1) * 100.0 / dirs.Length)
                    });
                    return SelectExtensionInfo(owner, ExtensionType.Extension, path);
                })
                .Where(i => i != null)
                .ToList();
        }

        public IList<LocalExtensionInfo> GetSkins(Cmdlet owner)
        {
            var dirs = Directory.GetDirectories(Path.Combine(RootPath, "skins"));
            return dirs.Select((path, i) =>
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

        private readonly Dictionary<string, Regex> variableValueRegexCache = new Dictionary<string, Regex>();

        private static Regex BuildStringVariableValueRegex(string name)
        {
            return new Regex(@"\$" + name + @"\s*=\s*(['""])(?<V>([^\'""]|\\[\\rnvt'""])*)\1");
        }

        private string TryGetPhpVariableValue(string name, string expression)
        {
            if (!variableValueRegexCache.TryGetValue(name, out var matcher))
                variableValueRegexCache.Add(name, matcher = BuildStringVariableValueRegex(name));
            var match = matcher.Match(expression);
            if (match.Success)
                return match.Groups["V"].Value;
            return null;
        }

        public LocalSiteInfo GetSiteInfo(Cmdlet owner, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var info = new LocalSiteInfo();

            void ParseSettingsPhp(string localPath, bool required, int progressOffset, int progressLength)
            {
                var path = Path.Combine(RootPath, localPath);
                var localProgress = new ProgressRecord(20, "Get local MediaWiki installation information.", "Load " + localPath)
                {
                    PercentComplete = progressOffset
                };
                if (!required && !File.Exists(path))
                {
                    owner.WriteWarning($"Cannot find {localPath}.");
                    return;
                }
                ct.ThrowIfCancellationRequested();
                using (var sr = File.OpenText(path))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        ct.ThrowIfCancellationRequested();
                        var percent = progressOffset + (int)((double)progressLength * sr.BaseStream.Position / sr.BaseStream.Length);
                        if (localProgress.PercentComplete != percent)
                        {
                            localProgress.PercentComplete = percent;
                            owner.WriteProgress(localProgress);
                        }
                        if (!line.Contains('$'))
                            continue;
                        var value = TryGetPhpVariableValue("wgSitename", line);
                        if (value != null)
                            info.SiteName = value;
                        value = TryGetPhpVariableValue("wgVersion", line);
                        if (value != null)
                            info.Version = value;
                    }
                }
            }

            ParseSettingsPhp("includes/DefaultSettings.php", true, 0, 50);
            ParseSettingsPhp("LocalSettings.php", false, 50, 50);

            return info;
        }

        public static bool CheckMwRootFolder(string path)
        {
            if (!File.Exists(Path.Combine(path, "index.php")))
                return false;
            if (!Directory.Exists(Path.Combine(path, "extensions")))
                return false;
            if (!Directory.Exists(Path.Combine(path, "skins")))
                return false;
            if (!Directory.Exists(Path.Combine(path, "includes")))
                return false;
            return true;
        }

        internal static void AssertMwRootFolder(string path, string paramName)
        {
            if (!CheckMwRootFolder(path))
                throw new ArgumentException("Specified path is not a valid MediaWiki installation.", paramName);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return RootPath;
        }
    }

}
