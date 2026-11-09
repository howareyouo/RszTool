namespace RszTool.App.ViewModels
{
    /// <summary>
    /// 内容被改动后能自己通知界面的节点
    /// </summary>
    public interface IItemsProvider
    {
        void NotifyItemsChanged();
    }
}