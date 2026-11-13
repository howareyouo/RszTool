using RszTool.App.ViewModels;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RszTool.App.Views
{
    /// <summary>
    /// RszFileView.xaml 的交互逻辑
    /// </summary>
    public partial class RszFileView : UserControl
    {
        /// <summary>
        /// 主动把焦点送进输入框时置位，用于放行 <see cref="TreeView_PreviewGotKeyboardFocus"/> 的拦截
        /// </summary>
        private bool focusEditor;

        public RszFileView()
        {
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is BaseRszFileViewModel viewModel)
            {
                viewModel.SelectedItem = e.NewValue;
            }
        }

        /// <summary>
        /// 行内输入框默认不接受键盘焦点，只有鼠标点击和回车才进得去。
        /// 键盘导航（Tab、翻页等）把焦点往输入框送时退回行容器，否则焦点一进输入框，方向键就被它吃掉，选中项再也动不了。
        /// </summary>
        private void TreeView_PreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (focusEditor) return;
            if (e.NewFocus is not (TextBox or ComboBox or CheckBox or Button)) return;
            if (Mouse.LeftButton == MouseButtonState.Pressed) return;

            e.Handled = true;
            if (FindRow((DependencyObject)e.NewFocus) is TreeViewItem row && !row.IsKeyboardFocusWithin)
            {
                row.Focus();
            }
        }

        /// <summary>
        /// 上下方向键移动选中项，回车把焦点交给选中行的输入框。
        /// </summary>
        private void TreeView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.None) return;
            if (sender is not TreeView treeView) return;

            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                // 枚举行的下拉框还要用上下键切值
                if (Keyboard.FocusedElement is ComboBox) return;
                e.Handled = true;
                MoveSelection(treeView, e.Key == Key.Up ? -1 : 1);
            }
            else if (e.Key == Key.Enter)
            {
                if (Keyboard.FocusedElement is TextBox or ComboBox or CheckBox or Button) return;
                if (FocusRowEditor(FindSelectedRow(treeView))) e.Handled = true;
            }
        }

        private static void MoveSelection(TreeView treeView, int offset)
        {
            var rows = new List<TreeViewItem>();
            CollectVisibleRows(treeView, rows);

            int index = rows.FindIndex(row => row.IsSelected);
            if (index < 0) return;

            int targetIndex = index + offset;
            if (targetIndex < 0 || targetIndex >= rows.Count) return;

            TreeViewItem target = rows[targetIndex];
            target.IsSelected = true;
            target.Focus();
            target.BringIntoView();
        }

        private static TreeViewItem? FindSelectedRow(TreeView treeView)
        {
            var rows = new List<TreeViewItem>();
            CollectVisibleRows(treeView, rows);
            return rows.Find(row => row.IsSelected);
        }

        /// <summary>
        /// 行的第一个编辑控件，文本框优先，其次下拉框、勾选框
        /// </summary>
        private bool FocusRowEditor(TreeViewItem? row)
        {
            if (row == null) return false;

            var editors = new List<Control>();
            CollectEditors(row, editors);

            Control? editor = editors.Find(control => control is TextBox);
            editor ??= editors.Find(control => control is ComboBox);
            editor ??= editors.Find(control => control is CheckBox);
            if (editor == null && editors.Count > 0) editor = editors[0];
            if (editor == null) return false;

            focusEditor = true;
            try
            {
                return editor.Focus();
            }
            finally
            {
                focusEditor = false;
            }
        }

        private static void CollectVisibleRows(ItemsControl parent, List<TreeViewItem> rows)
        {
            foreach (object item in parent.Items)
            {
                if (parent.ItemContainerGenerator.ContainerFromItem(item) is not TreeViewItem row) continue;
                if (!row.IsVisible) continue;

                rows.Add(row);
                if (row.IsExpanded) CollectVisibleRows(row, rows);
            }
        }

        private static void CollectEditors(DependencyObject parent, List<Control> editors)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is TreeViewItem) continue;

                if (child is Control control && control.Focusable && control.IsVisible && control.IsEnabled)
                {
                    editors.Add(control);
                }
                CollectEditors(child, editors);
            }
        }

        private static TreeViewItem? FindRow(DependencyObject element)
        {
            DependencyObject? current = element;
            while (current != null)
            {
                if (current is TreeViewItem row) return row;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}
