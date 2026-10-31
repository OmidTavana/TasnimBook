using Microsoft.Extensions.DependencyInjection;

namespace TasnimBook.MAUI;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Resolve pages only after App.xaml has loaded its merged dictionaries.
        // MainPage uses application-level StaticResources during InitializeComponent.
        var mainPage = _services.GetRequiredService<MainPage>();
        var navigationBackground = (Color)Resources["Primary"];
        var navigationForeground = (Color)Resources["White"];

        return new Window(new NavigationPage(mainPage)
        {
            BarBackgroundColor = navigationBackground,
            BarTextColor = navigationForeground
        });
    }
}
