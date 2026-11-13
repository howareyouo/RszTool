using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using RszTool.App.Common;
using RszTool.App.ViewModels;

namespace RszTool.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // 双击关联文件时进程由 explorer 启动，工作目录不一定是 exe 目录，
            // 而 rsz*.json、Data 和 SaveData 都按相对路径寻址，统一切到 exe 目录
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);

            SaveData? saveData = null;
            if (File.Exists(SaveData.JsonPath))
            {
                using FileStream fileStream = File.OpenRead(SaveData.JsonPath);
                saveData = JsonSerializer.Deserialize<SaveData>(fileStream);
                if (saveData != null) SaveData = saveData;
            }
            SaveData = saveData ?? new();
        }

        public static App Instance => (App)Current;
        public SaveData SaveData { get; }

        /// <summary>
        /// 命令行传入的资源文件，双击关联或拖到 exe 上时使用
        /// </summary>
        public IReadOnlyList<string> StartupFiles { get; private set; } = [];

        protected override void OnStartup(StartupEventArgs e)
        {
            StartupFiles = e.Args.Where(IsSupportedFile).ToList();
            base.OnStartup(e);

            ThemeManager.Init(SaveData.IsDarkTheme);
            // 添加全局异常处理程序
            SetupUnhandledExceptionHandling();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            JsonUtils.DumpJson(SaveData.JsonPath, SaveData);
        }

        private void SetupUnhandledExceptionHandling()
        {
            // Catch exceptions from all threads in the AppDomain.
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
                ShowUnhandledException((Exception)args.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException", false);

            // Catch exceptions from each AppDomain that uses a task scheduler for async operations.
            TaskScheduler.UnobservedTaskException += (sender, args) =>
                ShowUnhandledException(args.Exception, "TaskScheduler.UnobservedTaskException", false);

            // Catch exceptions from a single specific UI dispatcher thread.
            Dispatcher.UnhandledException += (sender, args) =>
            {
                // If we are debugging, let Visual Studio handle the exception and take us to the code that threw it.
                if (!Debugger.IsAttached)
                {
                    args.Handled = true;
                    ShowUnhandledException(args.Exception, "Dispatcher.UnhandledException", true);
                }
            };
        }

        private static bool IsSupportedFile(string path)
        {
            if (!File.Exists(path)) return false;
            return RszUtils.GetFileType(path) is FileType.user or FileType.pfb or FileType.scn;
        }

        public static void ShowUnhandledException(Exception e, string unhandledExceptionType, bool promptUserForShutdown = false)
        {
            var messageBoxTitle = $"Unexpected Error Occurred: {unhandledExceptionType}";
            var messageBoxMessage = $"The following exception occurred:\n\n{e}";
            var messageBoxButtons = MessageBoxButton.OK;

            if (promptUserForShutdown)
            {
                messageBoxMessage += "\n\nShould we close it?";
                messageBoxButtons = MessageBoxButton.YesNo;
            }

            // Let the user decide if the app should die or not (if applicable).
            if (MessageBox.Show(Application.Current.MainWindow, messageBoxMessage, messageBoxTitle, messageBoxButtons) == MessageBoxResult.Yes &&
                promptUserForShutdown)
            {
                Application.Current.Shutdown();
            }
        }
    }
}
