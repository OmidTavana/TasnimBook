using TasnimBook.MAUI.Services;

namespace TasnimBook.MAUI.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly ThemeService _theme;
    private readonly AppUpdateService _updates;

    public SettingsViewModel(ThemeService theme, AppUpdateService updates)
    {
        _theme = theme;
        _updates = updates;
        _theme.ThemeChanged += OnThemeChanged;
        _updates.StateChanged += OnUpdateChanged;

        CheckCommand = new AsyncCommand(CheckAsync, () => !IsUpdateBusy);
        DownloadCommand = new AsyncCommand(DownloadAsync, () => CanDownload);
        InstallCommand = new AsyncCommand(InstallAsync, () => CanInstall);
    }

    public bool IsLightMode
    {
        get => _theme.CurrentTheme == AppThemePreference.Light;
        set { if (value) _theme.SetTheme(AppThemePreference.Light); }
    }

    public bool IsDarkMode
    {
        get => _theme.CurrentTheme == AppThemePreference.Dark;
        set { if (value) _theme.SetTheme(AppThemePreference.Dark); }
    }

    public string CurrentVersion => _updates.CurrentVersion;
    public string UpdateStatus => _updates.StatusMessage;
    public string? AvailableVersion => _updates.AvailableVersion;
    public string? ReleaseNotes => _updates.ReleaseNotes;
    public double DownloadProgress => _updates.DownloadProgress;
    public string DownloadPercent => $"{Math.Round(_updates.DownloadProgress * 100):0}٪";
    public bool IsUpdateBusy => _updates.IsBusy;
    public bool IsChecking => _updates.State == AppUpdateState.Checking;
    public bool IsDownloading => _updates.State == AppUpdateState.Downloading;
    public bool IsProgressVisible => IsDownloading || _updates.State == AppUpdateState.Downloaded;
    public bool HasAvailableVersion => _updates.State is AppUpdateState.Available or AppUpdateState.Downloading or AppUpdateState.Downloaded or AppUpdateState.Installing;
    public bool CanDownload => _updates.CanDownload && !IsUpdateBusy;
    public bool CanInstall => _updates.CanInstall && !IsUpdateBusy;
    public bool HasError => _updates.State == AppUpdateState.Failed;
    public bool IsUpToDate => _updates.State == AppUpdateState.UpToDate;

    public AsyncCommand CheckCommand { get; }
    public AsyncCommand DownloadCommand { get; }
    public AsyncCommand InstallCommand { get; }

    public Task OnAppearingAsync()
    {
        RefreshAll();
        return Task.CompletedTask;
    }

    private Task CheckAsync() => _updates.CheckForUpdateAsync(force: true);
    private Task DownloadAsync() => _updates.DownloadUpdateAsync();
    private Task InstallAsync() => _updates.InstallUpdateAsync();

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(IsLightMode));
        OnPropertyChanged(nameof(IsDarkMode));
    }

    private void OnUpdateChanged(object? sender, EventArgs e) => RefreshAll();

    private void RefreshAll()
    {
        OnPropertyChanged(nameof(CurrentVersion));
        OnPropertyChanged(nameof(UpdateStatus));
        OnPropertyChanged(nameof(AvailableVersion));
        OnPropertyChanged(nameof(ReleaseNotes));
        OnPropertyChanged(nameof(DownloadProgress));
        OnPropertyChanged(nameof(DownloadPercent));
        OnPropertyChanged(nameof(IsUpdateBusy));
        OnPropertyChanged(nameof(IsChecking));
        OnPropertyChanged(nameof(IsDownloading));
        OnPropertyChanged(nameof(IsProgressVisible));
        OnPropertyChanged(nameof(HasAvailableVersion));
        OnPropertyChanged(nameof(CanDownload));
        OnPropertyChanged(nameof(CanInstall));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(IsUpToDate));
        CheckCommand.NotifyCanExecuteChanged();
        DownloadCommand.NotifyCanExecuteChanged();
        InstallCommand.NotifyCanExecuteChanged();
    }

}
