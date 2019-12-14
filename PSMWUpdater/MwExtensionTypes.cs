﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PSMWUpdater
{

    public enum ExtensionType
    {
        Unknown,
        Extension,
        Skin
    }

    public struct ExtensionName
    {

        public ExtensionName(string name, ExtensionType type)
        {
            Name = name;
            Type = type;
        }

        public static ExtensionName Parse(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(expr));
            expr = expr.Trim();
            var parts = expr.Split(new[] { ';' }, 2);
            if (parts.Length == 1)
                return new ExtensionName(parts[0], ExtensionType.Unknown);
            return new ExtensionName(parts[1], (ExtensionType)Enum.Parse(typeof(ExtensionType), parts[0], true));
        }

        public string Name { get; set; }

        public ExtensionType Type { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            if (Type == ExtensionType.Unknown)
                return Name;
            return Type + ":" + Name;
        }

    }

    public class ExtensionInfo
    {

        public ExtensionInfo(ExtensionName name)
        {
            Name = name;
        }

        public ExtensionName Name { get; }

    }

    public class LocalExtensionInfo : ExtensionInfo
    {

        internal LocalExtensionInfo(ExtensionName name, string rootPath)
            : base(name)
        {
            IsEmpty = !Directory.EnumerateFiles(rootPath).Any();
            LastWriteTime = IsEmpty ? DateTimeOffset.MinValue : Directory
                .EnumerateFiles(rootPath)
                .Select(fileName => (DateTimeOffset)File.GetLastWriteTime(fileName))
                .Min();
        }

        public DateTimeOffset LastWriteTime { get; }

        public bool IsEmpty { get; }

    }

    public class ExtensionBranchInfo
    {
        public ExtensionBranchInfo(ExtensionName extensionName, string branchName, string url)
        {
            ExtensionName = extensionName;
            BranchName = branchName;
            Url = url;
        }

        public ExtensionName ExtensionName { get; }

        public string BranchName { get; }

        public string Url { get; }

    }

}