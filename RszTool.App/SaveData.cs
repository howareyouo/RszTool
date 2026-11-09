using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;
using RszTool.Common;

namespace RszTool.App
{
    public class SaveData
    {
        public const string JsonPath = "RszTool.App.SaveData.json";

        [JsonConverter(typeof(EnumJsonConverter<GameName>))]
        public GameName GameName { get; set; } = GameName.re4;
        public ObservableCollection<string> RecentFiles { get; set; } = new();
        public List<string> OpenedFolders { get; set; } = new();
        public ContextIDData LastContextID { get; set; } = new();
        public bool IsDarkTheme { get; set; }
        public WindowPlacement Window { get; set; } = new();

        public void AddRecentFile(string path)
        {
            int recentIndex = RecentFiles.IndexOf(path);
            if (recentIndex >= 0)
            {
                RecentFiles.Move(recentIndex, 0);
            }
            else
            {
                RecentFiles.Insert(0, path);
            }
        }
    }


    public class ContextIDData : INotifyPropertyChanged
    {
        public int Group { get; set; }
        public int Index { get; set; }

        public string Text => $"Last ContextID Index: {Index} Group: {Group}";

        public event PropertyChangedEventHandler? PropertyChanged;
    }


    /// <summary>
    /// 主窗口的位置和大小，用于下次启动时恢复
    /// </summary>
    public class WindowPlacement
    {
        public double? Left { get; set; }
        public double? Top { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }
        public bool Maximized { get; set; }
    }
}
