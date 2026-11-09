using System.Text.Json;

namespace RszTool
{
    /// <summary>
    /// Rsz json parser
    /// </summary>
    public class EnumParser
    {
        private static readonly Dictionary<string, EnumParser> instanceDict = new();

        public static EnumParser GetInstance(string jsonPath)
        {
            if (!instanceDict.TryGetValue(jsonPath, out var rszParser))
            {
                rszParser = new EnumParser(jsonPath);
                instanceDict[jsonPath] = rszParser;
            }
            return rszParser;
        }

        public EnumDict EnumDict { get; }

        private EnumParser(EnumDict enumDict)
        {
            EnumDict = enumDict;
        }

        /// <summary>
        /// 空的枚举表，没有 locale json 时使用
        /// </summary>
        public static EnumParser Empty { get; } = new(new EnumDict([]));

        /// <summary>
        /// 直接用 json 文本构建枚举表，不读磁盘，也不进入实例缓存
        /// </summary>
        public static EnumParser FromJson(string json)
        {
            return new(new EnumDict(JsonSerializer.Deserialize<Dictionary<string, EnumData>>(json) ?? []));
        }

        public EnumParser(string jsonPath)
        {
            if (File.Exists(jsonPath))
            {
                using FileStream fileStream = File.OpenRead(jsonPath);
                EnumDict = new(JsonSerializer.Deserialize<Dictionary<string, EnumData>>(fileStream) ?? []);
            }
            else
            {
                EnumDict = new([]);
            }

            // read in culture specific json
            string cultureJsonPath = Path.ChangeExtension(jsonPath, $".{Thread.CurrentThread.CurrentCulture}.json");
            if (File.Exists(cultureJsonPath))
            {
                using FileStream fileStream = File.OpenRead(cultureJsonPath);
                EnumDict cultureDict = new(JsonSerializer.Deserialize<Dictionary<string, EnumData>>(fileStream) ?? []);
                if (cultureDict.Dict.Count > 0)
                {
                    foreach (var item in cultureDict.Dict)
                    {
                        EnumDict.Dict[item.Key] = item.Value;
                    }
                }
            }
        }
    }

    public record EnumDict(Dictionary<string, EnumData> Dict)
    {
        public EnumData? this[string name] => Dict.TryGetValue(name, out var data) ? data : null;
    }

    public record EnumData(EnumItem[] Items, bool Flags);
    public record EnumItem(string Name, ulong Value);
}
