using Microsoft.Maui.Controls.Shapes;

namespace TasnimBook.MAUI.Services;

public enum AppMessageType
{
    Information,
    Success,
    Warning,
    Error
}

public sealed class InAppMessageService
{
    private const string HostClassId = "TasnimBook.InAppMessageHost";
    private readonly SemaphoreSlim _toastQueue = new(1, 1);
    private readonly SemaphoreSlim _dialogQueue = new(1, 1);

    public async Task ShowToastAsync(
        string message,
        AppMessageType type = AppMessageType.Information,
        TimeSpan? duration = null)
    {
        await _toastQueue.WaitAsync();
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var page = GetCurrentContentPage();
                if (page is null) return;

                var host = EnsureOverlayHost(page);
                var dismissed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var toast = BuildToast(message, type, dismissed);
                toast.Opacity = 0;
                toast.TranslationY = -18;
                toast.ZIndex = 1000;
                host.Children.Add(toast);

                await Task.WhenAll(
                    toast.FadeToAsync(1, 160, Easing.CubicOut),
                    toast.TranslateToAsync(0, 0, 190, Easing.CubicOut));

                await Task.WhenAny(
                    Task.Delay(duration ?? TimeSpan.FromSeconds(type == AppMessageType.Error ? 6 : 4)),
                    dismissed.Task);

                await Task.WhenAll(
                    toast.FadeToAsync(0, 130, Easing.CubicIn),
                    toast.TranslateToAsync(0, -12, 130, Easing.CubicIn));
                host.Children.Remove(toast);
            });
        }
        finally
        {
            _toastQueue.Release();
        }
    }

    public async Task ShowDialogAsync(
        string title,
        string message,
        string accept = "باشه",
        AppMessageType type = AppMessageType.Information)
    {
        await ShowDialogCoreAsync(title, message, accept, null, type);
    }

    public Task<bool> ShowAcknowledgementAsync(
        string title,
        string message,
        string accept = "باشه",
        AppMessageType type = AppMessageType.Information) =>
        ShowDialogCoreAsync(title, message, accept, null, type);

    public Task<bool> ConfirmAsync(
        string title,
        string message,
        string accept,
        string cancel,
        AppMessageType type = AppMessageType.Warning) =>
        ShowDialogCoreAsync(title, message, accept, cancel, type);

    private async Task<bool> ShowDialogCoreAsync(
        string title,
        string message,
        string accept,
        string? cancel,
        AppMessageType type)
    {
        await _dialogQueue.WaitAsync();
        try
        {
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var page = GetCurrentPage();
                if (page is null) return false;

                var dialog = new MessageDialogPage(title, message, accept, cancel, type);
                await page.Navigation.PushModalAsync(dialog, false);
                await dialog.AnimateInAsync();
                var result = await dialog.Result;
                if (dialog.Navigation.ModalStack.Contains(dialog))
                    await dialog.Navigation.PopModalAsync(false);
                return result;
            });
        }
        finally
        {
            _dialogQueue.Release();
        }
    }

    private static Grid EnsureOverlayHost(ContentPage page)
    {
        if (page.Content is Grid existing && existing.ClassId == HostClassId)
            return existing;

        var original = page.Content;
        page.Content = null;
        var host = new Grid { ClassId = HostClassId };
        if (original is not null) host.Children.Add(original);
        page.Content = host;
        return host;
    }

    private static Border BuildToast(
        string message,
        AppMessageType type,
        TaskCompletionSource dismissed)
    {
        var palette = GetPalette(type);
        var close = new Button
        {
            Text = "×",
            FontSize = 22,
            TextColor = palette.Foreground,
            BackgroundColor = Colors.Transparent,
            WidthRequest = 42,
            HeightRequest = 42,
            Padding = 0,
            CornerRadius = 21
        };
        close.Clicked += (_, _) => dismissed.TrySetResult();

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };
        grid.Add(new Label
        {
            Text = palette.Icon,
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            TextColor = palette.Foreground,
            VerticalTextAlignment = TextAlignment.Center,
            WidthRequest = 28,
            HorizontalTextAlignment = TextAlignment.Center
        }, 0);
        grid.Add(new Label
        {
            Text = message,
            FontSize = 14,
            TextColor = palette.Foreground,
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.WordWrap,
            FlowDirection = FlowDirection.RightToLeft
        }, 1);
        grid.Add(close, 2);

        return new Border
        {
            Content = grid,
            BackgroundColor = palette.Background,
            Stroke = palette.Stroke,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Padding = new Thickness(14, 8),
            Margin = new Thickness(16, 14),
            MaximumWidthRequest = 560,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Start,
            Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Color.FromArgb("35000000")),
                Offset = new Point(0, 5),
                Radius = 14,
                Opacity = 0.35f
            }
        };
    }

    private static ContentPage? GetCurrentContentPage() => GetCurrentPage() as ContentPage;

    private static Page? GetCurrentPage()
    {
        Page? page = Application.Current?.Windows.FirstOrDefault()?.Page;
        while (page is not null)
        {
            if (page.Navigation.ModalStack.LastOrDefault() is { } modal)
            {
                page = modal;
                continue;
            }
            if (page is NavigationPage navigation)
            {
                page = navigation.CurrentPage;
                continue;
            }
            if (page is Shell shell)
            {
                page = shell.CurrentPage;
                continue;
            }
            break;
        }
        return page;
    }

    private static MessagePalette GetPalette(AppMessageType type)
    {
        var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
        return (type, dark) switch
        {
            (AppMessageType.Success, false) => new("✓", Color.FromArgb("#E8F6EE"), Color.FromArgb("#166534"), Color.FromArgb("#86C99F")),
            (AppMessageType.Success, true) => new("✓", Color.FromArgb("#173D2A"), Color.FromArgb("#D9FBE7"), Color.FromArgb("#3B8E5A")),
            (AppMessageType.Error, false) => new("!", Color.FromArgb("#FDECEC"), Color.FromArgb("#9F1C1C"), Color.FromArgb("#E9A0A0")),
            (AppMessageType.Error, true) => new("!", Color.FromArgb("#461F22"), Color.FromArgb("#FFE1E1"), Color.FromArgb("#A94D55")),
            (AppMessageType.Warning, false) => new("!", Color.FromArgb("#FFF5DD"), Color.FromArgb("#73510D"), Color.FromArgb("#E4C169")),
            (AppMessageType.Warning, true) => new("!", Color.FromArgb("#443717"), Color.FromArgb("#FFF0C2"), Color.FromArgb("#9B7B2A")),
            (AppMessageType.Information, true) => new("i", Color.FromArgb("#183A42"), Color.FromArgb("#DDF7FF"), Color.FromArgb("#3A8190")),
            _ => new("i", Color.FromArgb("#E8F3F6"), Color.FromArgb("#174F5A"), Color.FromArgb("#84BCC7"))
        };
    }

    private sealed record MessagePalette(string Icon, Color Background, Color Foreground, Color Stroke);

    private sealed class MessageDialogPage : ContentPage
    {
        private readonly TaskCompletionSource<bool> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Border _card;

        public Task<bool> Result => _result.Task;

        public MessageDialogPage(
            string title,
            string message,
            string accept,
            string? cancel,
            AppMessageType type)
        {
            BackgroundColor = Color.FromArgb("99000000");
            FlowDirection = FlowDirection.RightToLeft;
            Padding = new Thickness(20);
            NavigationPage.SetHasNavigationBar(this, false);

            var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
            var palette = GetPalette(type);
            var buttons = new Grid
            {
                ColumnDefinitions = cancel is null
                    ? [new ColumnDefinition(GridLength.Star)]
                    : [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)],
                ColumnSpacing = 10
            };

            var acceptButton = CreateButton(accept, palette.Foreground, palette.Background);
            acceptButton.Clicked += (_, _) => _result.TrySetResult(true);
            buttons.Add(acceptButton, 0);

            if (cancel is not null)
            {
                var cancelButton = CreateButton(
                    cancel,
                    dark ? Color.FromArgb("#D9E5E1") : Color.FromArgb("#33443F"),
                    dark ? Color.FromArgb("#303C38") : Color.FromArgb("#EDF1EF"));
                cancelButton.Clicked += (_, _) => _result.TrySetResult(false);
                buttons.Add(cancelButton, 1);
            }

            _card = new Border
            {
                MaximumWidthRequest = 520,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Padding = new Thickness(22),
                StrokeThickness = 1,
                Stroke = dark ? Color.FromArgb("#43534D") : Color.FromArgb("#DCE4E0"),
                BackgroundColor = dark ? Color.FromArgb("#1C2723") : Colors.White,
                StrokeShape = new RoundRectangle { CornerRadius = 24 },
                Shadow = new Shadow
                {
                    Brush = new SolidColorBrush(Color.FromArgb("50000000")),
                    Offset = new Point(0, 8),
                    Radius = 24,
                    Opacity = 0.5f
                },
                Content = new VerticalStackLayout
                {
                    Spacing = 16,
                    Children =
                    {
                        new Border
                        {
                            WidthRequest = 48,
                            HeightRequest = 48,
                            HorizontalOptions = LayoutOptions.End,
                            BackgroundColor = palette.Background,
                            StrokeThickness = 0,
                            StrokeShape = new RoundRectangle { CornerRadius = 24 },
                            Content = new Label
                            {
                                Text = palette.Icon,
                                TextColor = palette.Foreground,
                                FontAttributes = FontAttributes.Bold,
                                FontSize = 24,
                                HorizontalTextAlignment = TextAlignment.Center,
                                VerticalTextAlignment = TextAlignment.Center
                            }
                        },
                        new Label
                        {
                            Text = title,
                            FontSize = 21,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = dark ? Color.FromArgb("#F4F8F6") : Color.FromArgb("#17342C"),
                            HorizontalTextAlignment = TextAlignment.End
                        },
                        new Label
                        {
                            Text = message,
                            FontSize = 15,
                            LineHeight = 1.35,
                            TextColor = dark ? Color.FromArgb("#C6D3CE") : Color.FromArgb("#5D6965"),
                            HorizontalTextAlignment = TextAlignment.End
                        },
                        buttons
                    }
                }
            };

            Content = new Grid { Children = { _card } };
        }

        public async Task AnimateInAsync()
        {
            _card.Opacity = 0;
            _card.Scale = 0.96;
            await Task.WhenAll(
                _card.FadeToAsync(1, 160, Easing.CubicOut),
                _card.ScaleToAsync(1, 190, Easing.CubicOut));
        }

        protected override bool OnBackButtonPressed()
        {
            _result.TrySetResult(false);
            return true;
        }

        protected override void OnDisappearing()
        {
            _result.TrySetResult(false);
            base.OnDisappearing();
        }

        private static Button CreateButton(string text, Color foreground, Color background) => new()
        {
            Text = text,
            TextColor = foreground,
            BackgroundColor = background,
            FontAttributes = FontAttributes.Bold,
            FontSize = 15,
            CornerRadius = 14,
            HeightRequest = 48
        };
    }
}
