using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace TasnimBook.MAUI.ViewModels;

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); return true;
    }
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class AsyncCommand : ICommand
{
    private readonly Func<Task> _execute;
    private bool _running;
    public AsyncCommand(Func<Task> execute) => _execute = execute;
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => !_running;
    public async void Execute(object? parameter)
    {
        if (_running) return;
        _running = true; CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try { await _execute(); } finally { _running = false; CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
    }
}
