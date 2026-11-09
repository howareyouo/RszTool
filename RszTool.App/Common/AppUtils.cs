using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace RszTool.App.Common
{
    public static class AppUtils
    {
        public static void TryAction(Action action, [CallerMemberName] string name = "")
        {
            Guard(action, e => App.ShowUnhandledException(e, name));
        }

        public static void TryActionSimple(Action action)
        {
            Guard(action, e => MessageBoxUtils.Error(e.Message));
        }

        /// <summary>
        /// 调试时让异常直接抛出，否则交给调用方处理
        /// </summary>
        private static void Guard(Action action, Action<Exception> onError)
        {
            if (Debugger.IsAttached)
            {
                action();
                return;
            }
            try
            {
                action();
            }
            catch (Exception e)
            {
                onError(e);
            }
        }
    }
}
