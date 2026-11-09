using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RszTool.App.ViewModels;

namespace RszTool.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version!;
            Title = $"{Title} v{version.Major}.{version.Minor}.{version.Build} - By chenstack";

            // XAML 里的 Width/Height 要在 InitializeComponent 之后才会生效，
            // 而 SourceInitialized 早于首次绘制，在这里恢复可以避免窗口先闪一下默认尺寸
            SourceInitialized += (_, _) => RestorePlacement();
            Closing += OnClosing;
        }

        /// <summary>
        /// 恢复上次的窗口位置和大小，显示器数量或分辨率变过时退回默认值
        /// </summary>
        private void RestorePlacement()
        {
            WindowPlacement placement = App.Instance.SaveData.Window;
            if (placement.Width is not double width || placement.Height is not double height) return;
            if (width <= 0 || height <= 0) return;

            WindowStartupLocation = WindowStartupLocation.Manual;
            Width = width;
            Height = height;

            if (placement.Left is double left && placement.Top is double top && IsOnAnyScreen(new Rect(left, top, width, height)))
            {
                Left = left;
                Top = top;
            }
            else
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            // 最大化要放在最后，否则上面的尺寸赋值会把窗口拉回普通状态
            if (placement.Maximized) WindowState = WindowState.Maximized;
        }

        /// <summary>
        /// 判断保存的位置是否还落在虚拟桌面的可见区域内，换显示器或改分辨率后可能已经不可见
        /// </summary>
        private static bool IsOnAnyScreen(Rect bounds)
        {
            Rect virtualScreen = new(
                SystemParameters.VirtualScreenLeft,
                SystemParameters.VirtualScreenTop,
                SystemParameters.VirtualScreenWidth,
                SystemParameters.VirtualScreenHeight);
            Rect overlap = Rect.Intersect(virtualScreen, bounds);
            return !overlap.IsEmpty && overlap.Width > 0 && overlap.Height > 0;
        }

        private void SavePlacement()
        {
            WindowPlacement placement = App.Instance.SaveData.Window;
            if (WindowState == WindowState.Maximized)
            {
                placement.Maximized = true;
            }
            else if (WindowState == WindowState.Normal)
            {
                placement.Maximized = false;
                placement.Left = Left;
                placement.Top = Top;
                placement.Width = Width;
                placement.Height = Height;
            }
        }

        public void OnDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = false;
        }

        public void OnDrop(object sender, DragEventArgs e)
        {
            if (DataContext is not MainWindowModel mainWindowModel) return;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                mainWindowModel.OnDropFile(files);
            }
        }

        private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainWindowModel mainWindowModel)
            {
                if (!mainWindowModel.OnExit()) e.Cancel = true;
            }
            if (!e.Cancel) SavePlacement();
        }

        private void Tag_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowModel mainWindowModel &&
                mainWindowModel.SelectedTabItem != null)
            {
                IInputElement focusedControl = FocusManager.GetFocusedElement(this);
                if (focusedControl is TreeViewItem treeViewItem &&
                    e.Source is MenuItem menuItem && menuItem.Header is Ellipse ellipse)
                {
                    treeViewItem.Foreground = ellipse.Fill;
                }
            }
        }
    }
}
