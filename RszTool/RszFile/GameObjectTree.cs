namespace RszTool
{
    /// <summary>
    /// 游戏对象树的通用遍历，与具体文件格式无关
    /// </summary>
    public static class GameObjectTree
    {
        /// <summary>
        /// 收集对象、组件、子对象的实例
        /// </summary>
        public static void CollectInstances(IGameObjectData gameObject, List<RszInstance> rszInstances)
        {
            rszInstances.AddRange(IterInstances(gameObject));
        }

        /// <summary>
        /// 迭代对象、组件、子对象的实例
        /// </summary>
        public static IEnumerable<RszInstance> IterInstances(IGameObjectData gameObject)
        {
            yield return gameObject.Instance!;
            foreach (var item in gameObject.Components)
            {
                yield return item;
            }
            foreach (var child in gameObject.GetChildren())
            {
                foreach (var item in IterInstances(child))
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// 迭代对象，可选是否带上子对象
        /// </summary>
        public static IEnumerable<T> IterObjects<T>(
            IEnumerable<T>? items, bool includeChildren = false) where T : IGameObjectData
        {
            if (items == null) yield break;
            foreach (var item in items)
            {
                yield return item;
                if (includeChildren)
                {
                    foreach (var child in IterObjects(item.GetChildren().Cast<T>(), includeChildren))
                    {
                        yield return child;
                    }
                }
            }
        }

        /// <summary>
        /// re4 chainsaw.ContextID 的类名
        /// </summary>
        public const string ContextIdClassName = "chainsaw.ContextID";

        /// <summary>
        /// 一处 chainsaw.ContextID 引用
        /// </summary>
        public record ContextIdRef(string Path, RszInstance Instance);

        /// <summary>
        /// 对象、组件、子对象里的所有 chainsaw.ContextID 引用
        /// </summary>
        public static IEnumerable<ContextIdRef> ContextIdRefs(IGameObjectData gameObject)
        {
            foreach (var component in gameObject.Components)
            {
                if (component.RSZUserData != null) continue;
                var fields = component.Fields;
                for (int i = 0; i < component.Values.Length; i++)
                {
                    if (fields[i].IsReference && component.Values[i] is RszInstance instanceField
                        && instanceField.RszClass.name == ContextIdClassName)
                    {
                        yield return new($"{gameObject}/{component}.{fields[i].name}", instanceField);
                    }
                }
            }
            foreach (var child in gameObject.GetChildren())
            {
                foreach (var item in ContextIdRefs(child))
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// 实例（含子实例）里的所有 chainsaw.ContextID 引用
        /// </summary>
        public static IEnumerable<ContextIdRef> ContextIdRefs(RszInstance instance)
        {
            List<RszInstanceFieldRecord> paths = new();
            foreach (var item in instance.Flatten(new() { Paths = paths }))
            {
                if (item.RSZUserData != null) continue;
                var fields = item.Fields;
                string? parentPath = null;
                for (int i = 0; i < item.Values.Length; i++)
                {
                    if (fields[i].IsReference && item.Values[i] is RszInstance instanceField
                        && instanceField.RszClass.name == ContextIdClassName)
                    {
                        parentPath ??= string.Join("/", paths.Select(x => x.ToString()));
                        yield return new($"{parentPath}/{item}.{fields[i].name}", instanceField);
                    }
                }
            }
        }
    }
}