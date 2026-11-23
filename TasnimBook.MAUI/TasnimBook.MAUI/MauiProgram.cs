using Microsoft.Extensions.Logging;
using TasnimBook.MAUI.Services;
using TasnimBook.MAUI.ViewModels;
#if ANDROID || IOS
using MauiNativePdfView;
#endif

namespace TasnimBook.MAUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        });

#if ANDROID || IOS
        builder.UseMauiNativePdfView();
#endif

        builder.Services.AddSingleton<ApiSessionService>();
        builder.Services.AddSingleton<AccountService>();
        builder.Services.AddSingleton<BookAssetService>();
        builder.Services.AddSingleton<ThemeService>();
        builder.Services.AddSingleton<InAppMessageService>();
        builder.Services.AddSingleton<AppNavigationService>();
        builder.Services.AddSingleton<BookCatalogService>();
        builder.Services.AddSingleton<CategoryCoverCacheService>();
        builder.Services.AddSingleton<AppUpdateService>();
        builder.Services.AddSingleton<AppNotificationService>();
        builder.Services.AddSingleton<HomeViewModel>();
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<SettingsViewModel>();
        builder.Services.AddSingleton<Views.SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
