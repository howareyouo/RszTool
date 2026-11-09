namespace RszTool
{
    /// <summary>
    /// 打开文件所需的一切：游戏名、版本、类定义、枚举表
    /// </summary>
    public class RszFileOption
    {
        public GameName GameName { get; set; }
        public GameVersion Version { get; set; }
        public RszParser RszParser { get; set; }
        public EnumParser EnumParser { get; set; }

        public RszFileOption(GameName gameName)
            : this(
                gameName,
                RszParser.GetInstance($"rsz{gameName}.json"),
                EnumParser.GetInstance($"Data\\Enums\\{gameName}_enum.json"))
        {
        }

        /// <summary>
        /// 用外部传入的类定义和枚举表构造，不读磁盘
        /// </summary>
        public RszFileOption(GameName gameName, RszParser rszParser, EnumParser enumParser)
        {
            GameName = gameName;
            Version = gameName switch
            {
                GameName.re4 => GameVersion.re4,
                GameName.re2 => GameVersion.re2,
                GameName.re2rt => GameVersion.re2rt,
                GameName.re3 => GameVersion.re3,
                GameName.re3rt => GameVersion.re3rt,
                GameName.re7 => GameVersion.re7,
                GameName.re7rt => GameVersion.re7rt,
                GameName.re8 => GameVersion.re8,
                GameName.dmc5 => GameVersion.dmc5,
                GameName.mhrise => GameVersion.mhrise,
                GameName.sf6 => GameVersion.sf6,
                _ => GameVersion.unknown,
            };
            RszParser = rszParser;
            EnumParser = enumParser;
        }
    }
}