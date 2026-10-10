using Microsoft.Extensions.Logging;
using TasnimBook.MAUI.Services;
using TasnimBook.MAUI.ViewModels;

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

        builder.Services.AddSingleton<ApiSessionService>();
        builder.Services.AddSingleton<BookCatalogService>();
        builder.Services.AddSingleton<HomeViewModel>();
        builder.Services.AddSingleton<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
