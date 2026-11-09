using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using RszTool.App.Common;
using RszTool.App.Resources;


namespace RszTool.App.ViewModels
{
    public abstract class BaseRszFileViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public abstract BaseRszFile File { get; }
        public string? FilePath => File.FileHandler.FilePath;
        public string? FileName
        {
            get
            {
                string? path = FilePath;
                return path != null ? Path.GetFileName(path) : null;
            }
        }
        private bool changed;

        public bool Changed
        {
            get => changed;
            private set
            {
                changed = value;
                HeaderChanged?.Invoke();
            }
        }

        /// <summary>
        /// 编辑入口：标记脏，并按需通知受影响的节点
        /// </summary>
        public void OnEdited(object? node = null)
        {
            Changed = true;
            (node as IItemsProvider)?.NotifyItemsChanged();
        }

        /// <summary>
        /// 插入实例：re4 chainsaw.ContextID 修正 + 标记脏
        /// </summary>
        public void OnInserted(RszInstance instance, object? node = null)
        {
            UpdateInstanceContextID(instance);
            OnEdited(node);
        }

        /// <summary>
        /// 插入游戏对象：re4 chainsaw.ContextID 修正 + 标记脏
        /// </summary>
        public void OnGameObjectInserted(IGameObjectData gameObject)
        {
            ContextIdFixup.UpdateContextID(RszFileOption!, gameObject);
            OnEdited();
        }

        /// <summary>
        /// 按搜索条件填充游戏对象搜索结果
        /// </summary>
        protected static void FillGameObjectSearchList<T>(
            GameObjectSearchViewModel search, ObservableCollection<T> list, IEnumerable<T> items)
            where T : IGameObjectData
        {
            list.Clear();
            GameObjectFilter filter = new(search);
            if (!filter.Enable) return;
            foreach (var item in items)
            {
                if (filter.IsMatch(item))
                {
                    list.Add(item);
                }
            }
        }

        /// <summary>
        /// 询问要添加的组件类名，取消或空输入返回 null
        /// </summary>
        protected static string? AskClassName(string title, string message, string initial)
        {
            Views.InputDialog dialog = new()
            {
                Title = title,
                Message = message,
                InputText = initial,
                Owner = Application.Current.MainWindow,
            };
            if (dialog.ShowDialog() != true) return null;
            string className = dialog.InputText;
            if (string.IsNullOrWhiteSpace(className))
            {
                MessageBoxUtils.Error("ClassName is empty");
                return null;
            }
            return className;
        }

        public object? SelectedItem { get; set; }
        public InstanceSearchViewModel InstanceSearchViewModel { get; } = new();
        public ObservableCollection<RszInstance>? SearchInstanceList { get; set; }
        public RszFileOption? RszFileOption => File.GetRSZ()?.Option;
        public EnumDict? EnumDict => RszFileOption?.EnumParser?.EnumDict;

        public static RelayCommand CopyInstance => new(OnCopyInstance);
        public RelayCommand PasteInstance => new(OnPasteInstance);
        public static RelayCommand CopyNormalField => new(OnCopyNormalField);
        public RelayCommand PasteNormalField => new(OnPasteNormalField);
        public static RelayCommand ArrayItemCopy => new(OnArrayItemCopy);
        public RelayCommand ArrayItemRemove => new(OnArrayItemRemove);
        public RelayCommand ArrayItemDuplicate => new(OnArrayItemDuplicate);
        public RelayCommand ArrayItemDuplicateMulti => new(OnArrayItemDuplicateMulti);
        public RelayCommand ArrayItemPasteAfter => new(OnArrayItemPasteAfter);
        public RelayCommand ArrayItemPasteToSelf => new(OnArrayItemPasteToSelf);
        public RelayCommand ArrayItemNew => new(OnArrayItemNew);
        public RelayCommand ArrayItemPaste => new(OnArrayItemPaste);
        public RelayCommand SearchInstances => new(OnSearchInstances);

        /// <summary>
        /// 标题改变(SaveAs或者Changed)
        /// </summary>
        public event Action? HeaderChanged;

        /// <summary>
        /// Item改变了，但Item本身不支持Notify，Items用了Convert的情况，通知TreeView更新
        /// </summary>
        // public event Action<object>? TreeViewItemChanged;


        public virtual void PostRead() {}

        public bool Read()
        {
            if (File.Read())
            {
                PostRead();
                Changed = false;
                return true;
            }
            return false;
        }

        public bool Save()
        {
            bool result = File.Save();
            if (result)
            {
                Changed = false;
            }
            return result;
        }

        public bool SaveAs(string path)
        {
            bool result = File.SaveAs(path);
            if (result)
            {
                Changed = false;
                HeaderChanged?.Invoke();
                OnPropertyChanged(nameof(FilePath));
            }
            return result;
        }

        public bool Reopen()
        {
            // TODO check Changed
            File.FileHandler.Reopen();
            bool result = Read();
            OnPropertyChanged(nameof(TreeViewItems));
            return result;
        }

        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Update re4 chainsaw.ContextID
        /// </summary>
        /// <param name="gameObject"></param>
        private void UpdateInstanceContextID(RszInstance instance)
        {
            ContextIdFixup.UpdateInstanceContextID(RszFileOption!, instance);
        }

        private static void OnCopyInstance(object arg)
        {
            if (arg is RszInstance instance)
            {
                RszClipboard.Instance = instance;
            }
            else if (arg is RszFieldInstanceViewModel fieldInstanceViewModel)
            {
                RszClipboard.Instance = fieldInstanceViewModel.Instance;
            }
            else if (arg is GameObejctComponentViewModel componentViewModel)
            {
                RszClipboard.Instance = componentViewModel.Instance;
            }
        }

        private void OnPasteInstance(object arg)
        {
            if (arg is RszInstance instance)
            {
                if (DoPasteInstance(instance))
                {
                    OnEdited();
                }
            }
            else if (arg is RszFieldInstanceViewModel fieldInstanceViewModel)
            {
                if (DoPasteInstance(fieldInstanceViewModel.Instance))
                {
                    OnEdited(fieldInstanceViewModel);
                }
            }
            else if (arg is GameObejctComponentViewModel componentViewModel)
            {
                if (DoPasteInstance(componentViewModel.Instance))
                {
                    OnEdited(componentViewModel);
                }
            }
        }

        private bool DoPasteInstance(RszInstance instance)
        {
            if (RszClipboard.Instance == null) return false;
            if (RszClipboard.Instance.RszClass != instance.RszClass)
            {
                MessageBoxUtils.Error(string.Format(Texts.RszClassMismatch, RszClipboard.Instance.RszClass.name, instance.RszClass.name));
                return false;
            }
            if (File.GetRSZ() is not RSZFile rsz) return false;
            bool result = rsz.InstanceCopyValues(instance, RszClipboard.Instance);
            if (result)
            {
                UpdateInstanceContextID(instance);
            }
            return result;
        }

        private static void OnCopyNormalField(object arg)
        {
            if (arg is RszFieldNormalViewModel item)
            {
                RszClipboard.NormalField = item.Field;
                RszClipboard.NormalValue = item.Value;
            }
        }

        private void OnPasteNormalField(object arg)
        {
            if (arg is RszFieldNormalViewModel item && RszClipboard.NormalField != null && RszClipboard.NormalValue != null)
            {
                if (RszClipboard.NormalField.type != item.Field.type)
                {
                    MessageBoxUtils.Error(string.Format(Texts.RszClassMismatch, RszClipboard.NormalField.type, item.Field.type));
                    return;
                }
                item.Value = RszClipboard.NormalValue;
                OnEdited();
            }
        }

        private static void OnArrayItemCopy(object arg)
        {
            if (arg is RszFieldArrayInstanceItemViewModel item)
            {
                RszClipboard.Instance = item.Instance;
            }
            else if (arg is RszFieldArrayNormalItemViewModel normalItem)
            {
                RszClipboard.NormalField = normalItem.Field;
                RszClipboard.NormalValue = normalItem.Value;
            }
        }

        /// <summary>
        /// 移除数组项
        /// </summary>
        /// <param name="arg"></param>
        private void OnArrayItemRemove(object arg)
        {
            if (arg is BaseRszFieldArrayItemViewModel item && File.GetRSZ() is RSZFile rsz)
            {
                rsz.ArrayRemoveItem(item.Values, item.Value);
                OnEdited(item.Array);
            }
        }

        /// <summary>
        /// 重复数组项
        /// </summary>
        /// <param name="arg"></param>
        private void OnArrayItemDuplicate(object arg)
        {
            if (File.GetRSZ() is not RSZFile rsz) return;
            if (arg is RszFieldArrayInstanceItemViewModel item)
            {
                var newItem = rsz.ArrayInsertInstance(item.Values, item.Instance, item.Index + 1);
                OnInserted(newItem, item.Array);
            }
            else if (arg is RszFieldArrayNormalItemViewModel normalItem)
            {
                rsz.ArrayInsertItem(normalItem.Values, normalItem.Value, normalItem.Index + 1);
                OnEdited(normalItem.Array);
            }
        }

        /// <summary>
        /// 重复多次数组项
        /// </summary>
        /// <param name="arg"></param>
        private void OnArrayItemDuplicateMulti(object arg)
        {
            if (File.GetRSZ() is not RSZFile rsz) return;
            Views.InputDialog dialog = new()
            {
                Title = Texts.Duplicate,
                Message = Texts.InputDulicateCount,
                InputText = "1",
                Owner = Application.Current.MainWindow,
            };
            // 显示对话框，并等待用户输入
            if (dialog.ShowDialog() != true) return;
            string userInput = dialog.InputText;
            int count = int.Parse(userInput);
            if (arg is RszFieldArrayInstanceItemViewModel item)
            {
                for (int i = 0; i < count; i++)
                {
                    var newItem = rsz.ArrayInsertInstance(item.Values, item.Instance, item.Index + 1);
                    UpdateInstanceContextID(newItem);
                }
                OnEdited(item.Array);
            }
            else if (arg is RszFieldArrayNormalItemViewModel normalItem)
            {
                for (int i = 0; i < count; i++)
                {
                    rsz.ArrayInsertItem(normalItem.Values, normalItem.Value, normalItem.Index + 1);
                }
                OnEdited(normalItem.Array);
            }
        }

        /// <summary>
        /// 在数组项后面粘贴
        /// </summary>
        /// <param name="arg"></param>
        private void OnArrayItemPasteAfter(object arg)
        {
            if (File.GetRSZ() is not RSZFile rsz) return;
            if (arg is RszFieldArrayInstanceItemViewModel item && RszClipboard.Instance != null)
            {
                if (RszClipboard.Instance.RszClass != item.Instance.RszClass)
                {
                    MessageBoxUtils.Error(string.Format(Texts.RszClassMismatch, RszClipboard.Instance.RszClass.name, item.Instance.RszClass.name));
                    return;
                }
                var newItem = rsz.ArrayInsertInstance(item.Values, RszClipboard.Instance, item.Index + 1);
                OnInserted(newItem, item.Array);
            }
            else if (arg is RszFieldArrayNormalItemViewModel normalItem
                && RszClipboard.NormalField != null && RszClipboard.NormalValue != null)
            {
                if (RszClipboard.NormalField.type != normalItem.Field.type)
                {
                    MessageBoxUtils.Error(string.Format(Texts.RszClassMismatch, RszClipboard.NormalField.type, normalItem.Field.type));
                    return;
                }
                rsz.ArrayInsertItem(normalItem.Values, RszClipboard.NormalValue, normalItem.Index + 1);
                OnEdited(normalItem.Array);
            }
        }

        /// <summary>
        /// 粘贴到自身
        /// </summary>
        /// <param name="arg"></param>
        private void OnArrayItemPasteToSelf(object arg)
        {
            if (arg is RszFieldArrayInstanceItemViewModel item)
            {
                if (DoPasteInstance(item.Instance))
                {
                    OnEdited(item);
                }
            }
            else if (arg is RszFieldArrayNormalItemViewModel normalItem
                && RszClipboard.NormalField != null && RszClipboard.NormalValue != null)
            {
                if (RszClipboard.NormalField.type != normalItem.Field.type)
                {
                    MessageBoxUtils.Error(string.Format(Texts.RszClassMismatch, RszClipboard.NormalField.type, normalItem.Field.type));
                    return;
                }
                normalItem.Value = RszClipboard.NormalValue;
                OnEdited();
            }
        }

        /// <summary>
        /// 数组新建项
        /// </summary>
        /// <param name="arg"></param>
        private void OnArrayItemNew(object arg)
        {
            if (arg is RszFieldArrayViewModel item && File.GetRSZ() is RSZFile rsz)
            {
                string? className = null;
                if (item.Field.IsReference)
                {
                    className = RszInstance.GetElementType(item.Field.original_type);
                    Views.InputDialog dialog = new()
                    {
                        Title = Texts.NewItem,
                        Message = Texts.InputClassName,
                        InputText = className,
                        Owner = Application.Current.MainWindow,
                    };
                    if (dialog.ShowDialog() != true) return;
                    className = dialog.InputText;
                }
                var newItem = RszInstance.CreateArrayItem(rsz.RszParser, item.Field, className);
                if (newItem == null) return;
                if (newItem is RszInstance instance)
                {
                    rsz.ArrayInsertInstance(item.Values, instance, clone: false);
                }
                else
                {
                    rsz.ArrayInsertItem(item.Values, newItem);
                }
                OnEdited(item);
            }
        }

        /// <summary>
        /// 粘贴到数组
        /// </summary>
        private void OnArrayItemPaste(object arg)
        {
            if (arg is RszFieldArrayViewModel item && RszClipboard.Instance != null &&
                File.GetRSZ() is RSZFile rsz)
            {
                string className = RszInstance.GetElementType(item.Field.original_type);
                if (className != RszClipboard.Instance.RszClass.name &&
                    !MessageBoxUtils.Confirm(string.Format(
                        Texts.RszClassMismatchConfirm, RszClipboard.Instance.RszClass.name, className)))
                {
                    return;
                }
                var newItem = rsz.ArrayInsertInstance(item.Values, RszClipboard.Instance);
                OnInserted(newItem, item);
            }
        }

        private void OnSearchInstances(object arg)
        {
            SearchInstanceList ??= new();
            SearchInstanceList.Clear();
            if (File.GetRSZ() is not RSZFile rsz) return;
            InstanceFilter filter = new(InstanceSearchViewModel);
            if (!filter.Enable) return;
            foreach (var instance in rsz.InstanceList)
            {
                if (filter.IsMatch(instance))
                {
                    SearchInstanceList.Add(instance);
                }
            }
        }

        public abstract IEnumerable<object> TreeViewItems { get; }
    }


    public class GameObejctComponentViewModel(IGameObjectData gameObject, RszInstance instance)
        : BaseViewModel, IItemsProvider
    {
        public IGameObjectData GameObject { get; } = gameObject;
        public RszInstance Instance { get; } = instance;

        public string Name => Instance.Name;
        public IEnumerable<object> Items => Converters.RszInstanceFieldsConverter.GetItems(Instance);

        public void NotifyItemsChanged()
        {
            OnPropertyChanged(nameof(Items));
        }

        public override string ToString()
        {
            return Instance.Name;
        }

        public static ObservableCollection<GameObejctComponentViewModel> MakeList(IGameObjectData gameObject)
        {
            ObservableCollection<GameObejctComponentViewModel> list = new();
            foreach (var item in gameObject.Components)
            {
                list.Add(new(gameObject, item));
            }
            gameObject.Components.CollectionChanged += (_, e) =>
            {
                ObjectModelUtils.SyncObservableCollection(list,
                    obj => new GameObejctComponentViewModel(gameObject, (RszInstance)obj), e);
            };
            return list;
        }
    }
}
