using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using RszTool.App.Common;

namespace RszTool.App.ViewModels
{
    public class FileExplorerViewModel
    {
        public ObservableCollection<RootDirectoryItem> Folders { get; } = new();
        public event Action<FileItem>? OnFileSelected;

        public RelayCommand RefreshRootDirectory => new(OnRefreshRootDirectory);
        public RelayCommand RemoveRootDirectory => new(OnRemoveRootDirectory);
        public RelayCommand OpenInExplorer => new(OnOpenInExplorer);

        public void Refresh()
        {
            foreach (var folder in Folders)
            {
                folder.Refresh();
            }
        }

        public void AddFolder(string path)
        {
            if (!Folders.Any(item => item.Path == path))
            {
                Folders.Add(new(path));
                App.Instance.SaveData.OpenedFolders.Add(path);
            }
        }

        public void SelectFile(FileItem fileItem)
        {
            OnFileSelected?.Invoke(fileItem);
        }

        private void OnRefreshRootDirectory(object arg)
        {
            if (arg is RootDirectoryItem rootDirectory)
            {
                rootDirectory.Refresh();
            }
        }

        private void OnRemoveRootDirectory(object arg)
        {
            if (arg is RootDirectoryItem rootDirectory)
            {
                Folders.Remove(rootDirectory);
                App.Instance.SaveData.OpenedFolders.Remove(rootDirectory.Path);
            }
        }

        private void OnOpenInExplorer(object arg)
        {
            if (arg is BaseFileItem item)
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{item.Path}\"");
            }
        }
    }


    public class BaseFileItem : INotifyPropertyChanged
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public bool IsExpanded { get; set; }

        protected BaseFileItem(string path)
        {
            Path = path;
            Name = System.IO.Path.GetFileName(path);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }


    public class FileItem(string path) : BaseFileItem(path)
    {
    }


    /// <summary>
    /// 目录树节点，只管界面状态，列举交给 FolderListing
    /// </summary>
    public class DirectoryItem(string path) : BaseFileItem(path)
    {
        private ObservableCollection<BaseFileItem>? children;

        public ObservableCollection<BaseFileItem> Children
        {
            get
            {
                if (children == null)
                {
                    children = new();
                    Refresh();
                }
                return children;
            }
        }

        /// <summary>
        /// 重建子节点，已有的节点实例保留，展开状态不丢
        /// </summary>
        public void Refresh()
        {
            if (children == null) return;
            Dictionary<string, BaseFileItem> existing = new();
            foreach (var item in children)
            {
                existing[item.Path] = item;
            }
            children.Clear();
            var entries = FolderListing.List(Path);
            foreach (var entry in entries)
            {
                if (!existing.TryGetValue(entry.Path, out var item))
                {
                    item = entry.IsDirectory ? new DirectoryItem(entry.Path) : new FileItem(entry.Path);
                }
                children.Add(item);
            }
            if (entries.Count == 1 && entries[0].IsDirectory && children[0] is DirectoryItem onlyDirectory)
            {
                onlyDirectory.IsExpanded = true;
            }
            // 只顺着已经展开过的子树往下走，未展开的节点仍是惰性
            foreach (var item in children)
            {
                if (item is DirectoryItem directory) directory.Refresh();
            }
        }
    }


    public class RootDirectoryItem(string path) : DirectoryItem(path)
    {
    }
}