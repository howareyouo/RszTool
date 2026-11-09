namespace RszTool.App.ViewModels
{
    /// <summary>
    /// re4 chainsaw.ContextID 在粘贴或新建后的修正
    /// </summary>
    public static class ContextIdFixup
    {
        /// <summary>
        /// 展示修正界面的入口，没有界面时静默跳过
        /// </summary>
        public static Action<IEnumerable<GameObjectContextID>>? Prompt { get; set; }

        public static void UpdateContextID(RszFileOption fileOption, IGameObjectData gameObject)
        {
            if (fileOption.GameName != GameName.re4) return;
            Show(GameObjectTree.ContextIdRefs(gameObject));
        }

        public static void UpdateInstanceContextID(RszFileOption fileOption, RszInstance instance)
        {
            if (fileOption.GameName != GameName.re4) return;
            Show(GameObjectTree.ContextIdRefs(instance));
        }

        private static void Show(IEnumerable<GameObjectTree.ContextIdRef> refs)
        {
            GameObjectContextID[] items =
                refs.Select(item => new GameObjectContextID(item.Path, item.Instance)).ToArray();
            if (items.Length > 0) Prompt?.Invoke(items);
        }
    }


    /// <summary>
    /// For re4 chainsaw.ContextID, when paste game object, update the ContextID
    /// </summary>
    public class GameObjectContextID
    {
        public GameObjectContextID(string name, RszInstance instance)
        {
            Name = name;
            if (instance.RszClass.name != GameObjectTree.ContextIdClassName)
            {
                throw new ArgumentException($"Expected {GameObjectTree.ContextIdClassName}");
            }
            int groupIndex = instance.RszClass.IndexOfField("_Group");
            int indexIndex = instance.RszClass.IndexOfField("_Index");
            RszFieldNormalViewModel groupViewModel = new(instance, groupIndex);
            RszFieldNormalViewModel indexViewModel = new(instance, indexIndex);
            Items = [
                groupViewModel,
                indexViewModel,
            ];

            groupViewModel.PropertyChanged += (o, e) => {
                App.Instance.SaveData.LastContextID.Group = (int)groupViewModel.Value;
            };
            indexViewModel.PropertyChanged += (o, e) => {
                App.Instance.SaveData.LastContextID.Index = (int)indexViewModel.Value;
            };
        }

        public string Name { get; set; }
        public RszFieldNormalViewModel[] Items { get; set; }
    }
}