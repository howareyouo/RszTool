namespace RszTool.App.ViewModels
{
    /// <summary>
    /// 唯一的剪贴板：实例、普通字段值、游戏对象
    /// </summary>
    public static class RszClipboard
    {
        public static RszInstance? Instance { get; set; }
        public static RszField? NormalField { get; set; }
        public static object? NormalValue { get; set; }
        public static PfbFile.GameObjectData? PfbGameObject { get; set; }
        public static ScnFile.GameObjectData? ScnGameObject { get; set; }

        public static void CopyGameObject(PfbFile.GameObjectData gameObject)
        {
            PfbGameObject = gameObject;
            ScnGameObject = null;
        }

        public static void CopyGameObject(ScnFile.GameObjectData gameObject)
        {
            ScnGameObject = gameObject;
            PfbGameObject = null;
        }

        /// <summary>
        /// 取用于prefab粘贴的对象，来自scn时按pfb转换
        /// </summary>
        public static PfbFile.GameObjectData? GetCopiedPfbGameObject()
        {
            if (PfbGameObject != null) return PfbGameObject;
            if (ScnGameObject != null) return PfbFile.GameObjectData.FromScnGameObject(ScnGameObject);
            return null;
        }

        /// <summary>
        /// 取用于scn粘贴的对象，来自pfb时按scn转换
        /// </summary>
        public static ScnFile.GameObjectData? GetCopiedScnGameObject()
        {
            if (ScnGameObject != null) return ScnGameObject;
            if (PfbGameObject != null) return ScnFile.GameObjectData.FromPfbGameObject(PfbGameObject);
            return null;
        }
    }
}