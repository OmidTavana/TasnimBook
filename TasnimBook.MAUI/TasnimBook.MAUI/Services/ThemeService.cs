namespace TasnimBook.MAUI.Services;

public enum AppThemePreference
{
    Light,
    Dark
}

public sealed class ThemeService
{
    private const string PreferenceKey = "TasnimBook.Appearance.Theme";

    public event EventHandler? ThemeChanged;

    public AppThemePreference CurrentTheme { get; private set; } = AppThemePreference.Light;

    public void ApplySavedTheme()
    {
        var stored = Preferences.Default.Get(PreferenceKey, nameof(AppThemePreference.Light));
        CurrentTheme = Enum.TryParse<AppThemePreference>(stored, out var theme)
            ? theme
            : AppThemePreference.Light;
        Apply(CurrentTheme, persist: false);
    }

    public void SetTheme(AppThemePreference theme) => Apply(theme, persist: true);

    private void Apply(AppThemePreference theme, bool persist)
    {
        CurrentTheme = theme;
        if (Application.Current is not null)
            Application.Current.UserAppTheme = theme == AppThemePreference.Dark
                ? AppTheme.Dark
                : AppTheme.Light;

        if (persist)
            Preferences.Default.Set(PreferenceKey, theme.ToString());

        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }
}
