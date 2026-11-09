using System.Windows.Input;

namespace RszTool.App.Common
{
    public class RelayCommand(Action<object> action) : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public Func<object, bool>? CanExecution { set; get; }
        public Action<object>? DoExecute { set; get; } = action;

        public bool CanExecute(object? parameter)
        {
            return CanExecution?.Invoke(parameter!) ?? true;
        }

        public void Execute(object? parameter)
        {
            DoExecute!.Invoke(parameter!);
        }
    }
}
