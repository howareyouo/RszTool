using System.IO;

namespace RszTool.App.Common
{
    /// <summary>
    /// 目录下的一个节点
    /// </summary>
    public record FolderEntry(string Path, bool IsDirectory);


    /// <summary>
    /// 目录列举，与界面状态无关
    /// </summary>
    public static class FolderListing
    {
        /// <summary>
        /// 界面认出的文件类型
        /// </summary>
        public static readonly HashSet<string> FileFilter = new() { ".pfb", ".scn", ".user" };

        private static readonly IComparer<FolderEntry> comparer = Comparer<FolderEntry>.Create(
            (x, y) => string.Compare(Path.GetFileName(x.Path), Path.GetFileName(y.Path)));

        /// <summary>
        /// 列出子目录和文件，目录在前，各自按名字排序
        /// </summary>
        public static List<FolderEntry> List(string path, ISet<string>? filter = null)
        {
            filter ??= FileFilter;
            List<FolderEntry> items = new();
            int directoryCount = 0;
            try
            {
                foreach (var childPath in Directory.EnumerateDirectories(path))
                {
                    items.Add(new FolderEntry(childPath, true));
                    directoryCount++;
                }
                foreach (var childPath in Directory.EnumerateFiles(path))
                {
                    RszUtils.GetFileExtension(childPath, out string extension, out _);
                    if (!filter.Contains(extension)) continue;
                    items.Add(new FolderEntry(childPath, false));
                }
            }
            catch (Exception e) when (e is UnauthorizedAccessException or DirectoryNotFoundException or IOException)
            {
                return items;
            }
            if (directoryCount > 0) items.Sort(0, directoryCount, comparer);
            if (items.Count > directoryCount) items.Sort(directoryCount, items.Count - directoryCount, comparer);
            return items;
        }
    }
}