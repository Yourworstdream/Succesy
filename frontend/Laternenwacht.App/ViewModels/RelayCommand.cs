using System.Windows.Input;

namespace Laternenwacht.App.ViewModels;

/// <summary>Einfaches ICommand, dessen Ausführbarkeit von WPF automatisch neu abgefragt wird.</summary>
internal sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    private readonly Action _execute = execute ?? throw new ArgumentNullException(nameof(execute));

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    public void Execute(object? parameter)
    {
        if (CanExecute(parameter))
        {
            _execute();
        }
    }

    public static void Refresh() => CommandManager.InvalidateRequerySuggested();
}
