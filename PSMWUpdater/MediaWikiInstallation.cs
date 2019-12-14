using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PSMWUpdater.Commands;

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

        public void Refresh()
        {
            InstalledExtensions = Directory
                .EnumerateDirectories(Path.Combine(RootPath, "extensions"))
                .Select(path => new LocalExtensionInfo(new ExtensionName(Path.GetFileName(path), ExtensionType.Extension), path))
                .ToList();
            InstalledSkins = Directory
                .EnumerateDirectories(Path.Combine(RootPath, "skins"))
                .Select(path => new LocalExtensionInfo(new ExtensionName(Path.GetFileName(path), ExtensionType.Skin), path))
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
