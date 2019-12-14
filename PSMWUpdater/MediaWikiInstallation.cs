using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PSMWUpdater
{
    public class MediaWikiInstallation
    {

        public MediaWikiInstallation(string rootPath)
        {
            RootPath = Path.GetFullPath(rootPath);
        }

        public string RootPath { get; }

        public IReadOnlyList<GeneralExtensionInfo> InstalledExtensions { get; private set; }

        public IReadOnlyList<GeneralExtensionInfo> InstalledSkins { get; private set; }
        
        public void Refresh()
        {
            InstalledExtensions = Directory
                .EnumerateDirectories(Path.Join(RootPath, "extensions"))
                .Select(path => new GeneralExtensionInfo(Path.GetFileName(path), path))
                .ToImmutableList();
            InstalledSkins = Directory
                .EnumerateDirectories(Path.Join(RootPath, "skins"))
                .Select(path => new GeneralExtensionInfo(Path.GetFileName(path), path))
                .ToImmutableList();
        }

        public static bool CheckMwRootFolder(string path)
        {
            if (!File.Exists(Path.Join(path, "index.php")))
                return false;
            if (!Directory.Exists(Path.Join(path, "extensions")))
                return false;
            if (!Directory.Exists(Path.Join(path, "skins")))
                return false;
            return true;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return RootPath;
        }
    }

    public class GeneralExtensionInfo
    {

        public GeneralExtensionInfo(string name, string rootPath)
        {
            Name = name;
            IsEmpty = !Directory.EnumerateFiles(rootPath).Any();
            LastWriteTime = IsEmpty ? DateTimeOffset.MinValue : Directory
                .EnumerateFiles(rootPath)
                .Select(fileName => (DateTimeOffset)File.GetLastWriteTime(fileName))
                .Min();
        }

        public string Name { get; }

        public DateTimeOffset LastWriteTime { get; }

        public bool IsEmpty { get; }

    }

}
