using System.Text.Json;

#if ANDROID || IOS
using MauiNativePdfView;
using MauiNativePdfView.Abstractions;
#elif WINDOWS
using Microsoft.Web.WebView2.Core;
#endif

namespace TasnimBook.MAUI.Controls;

public sealed class PdfDocumentLoadedEventArgs(int pageCount) : EventArgs
{
    public int PageCount { get; } = pageCount;
}

public sealed class PdfPageChangedEventArgs(int pageIndex, int pageCount) : EventArgs
{
    public int PageIndex { get; } = pageIndex;
    public int PageCount { get; } = pageCount;
}

/// <summary>
/// Uses a native viewer on Android/iOS and the bundled PDF.js viewer on Windows.
/// Page indexes exposed by this control are always zero-based.
/// </summary>
public sealed class PlatformPdfReader : ContentView
{
    public static readonly BindableProperty SourcePathProperty = BindableProperty.Create(
        nameof(SourcePath), typeof(string), typeof(PlatformPdfReader), string.Empty,
        propertyChanged: static (bindable, _, _) => ((PlatformPdfReader)bindable).LoadSource());

    public static readonly BindableProperty CurrentPageProperty = BindableProperty.Create(
        nameof(CurrentPage), typeof(int), typeof(PlatformPdfReader), 0,
        BindingMode.TwoWay,
        propertyChanged: static (bindable, _, value) => ((PlatformPdfReader)bindable).NavigateToPage((int)value));

    public static readonly BindableProperty ZoomProperty = BindableProperty.Create(
        nameof(Zoom), typeof(double), typeof(PlatformPdfReader), 1d,
        BindingMode.TwoWay,
        coerceValue: static (_, value) => Math.Clamp((double)value, 0.75, 3),
        propertyChanged: static (bindable, _, value) => ((PlatformPdfReader)bindable).ApplyZoom((double)value));

    private bool _documentReady;
    private bool _updatingFromViewer;

#if ANDROID || IOS
    private readonly PdfView _viewer;
#elif WINDOWS
    private readonly WebView _viewer;
    private Microsoft.UI.Xaml.Controls.WebView2? _platformViewer;
    private bool _webViewInitialized;
#else
    private readonly Label _viewer;
#endif

    public PlatformPdfReader()
    {
#if ANDROID || IOS
        _viewer = new PdfView
        {
            DisplayMode = PdfDisplayMode.SinglePageContinuous,
            ScrollOrientation = PdfScrollOrientation.Vertical,
            FitPolicy = FitPolicy.Width,
            EnableZoom = true,
            EnableSwipe = true,
            EnableLinkNavigation = true,
            MinZoom = 0.75f,
            MaxZoom = 3f,
            BackgroundColor = Color.FromArgb("#242424")
        };
        _viewer.DocumentLoaded += (_, args) =>
        {
            _documentReady = true;
            DocumentLoaded?.Invoke(this, new PdfDocumentLoadedEventArgs(args.PageCount));
            NavigateToPage(CurrentPage);
        };
        _viewer.PageChanged += (_, args) => ReportPageChanged(args.PageIndex, args.PageCount);
        _viewer.ZoomChanged += (_, args) =>
        {
            _updatingFromViewer = true;
            Zoom = args.Zoom;
            _updatingFromViewer = false;
        };
        _viewer.Error += (_, args) => Error?.Invoke(this, args.Message);
        Content = _viewer;
#elif WINDOWS
        _viewer = new WebView { BackgroundColor = Color.FromArgb("#242424") };
        _viewer.HandlerChanged += OnWindowsHandlerChanged;
        Content = _viewer;
#else
        _viewer = new Label
        {
            Text = "نمایش PDF در این پلتفرم پشتیبانی نمی‌شود.",
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };
        Content = _viewer;
#endif
    }

    public string SourcePath
    {
        get => (string)GetValue(SourcePathProperty);
        set => SetValue(SourcePathProperty, value);
    }

    public int CurrentPage
    {
        get => (int)GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    public double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public event EventHandler<PdfDocumentLoadedEventArgs>? DocumentLoaded;
    public event EventHandler<PdfPageChangedEventArgs>? PageChanged;
    public event EventHandler<string>? Error;

    public void GoToPage(int pageIndex) => CurrentPage = Math.Max(0, pageIndex);

    private void LoadSource()
    {
        _documentReady = false;
        if (string.IsNullOrWhiteSpace(SourcePath) || !File.Exists(SourcePath)) return;

#if ANDROID || IOS
        _viewer.DefaultPage = Math.Max(0, CurrentPage);
        _viewer.Source = PdfSource.FromFile(SourcePath);
#elif WINDOWS
        _ = LoadWindowsSourceAsync();
#endif
    }

    private void NavigateToPage(int pageIndex)
    {
        if (_updatingFromViewer || !_documentReady) return;
        pageIndex = Math.Max(0, pageIndex);

#if ANDROID || IOS
        if (_viewer.CurrentPage != pageIndex) _viewer.GoToPage(pageIndex);
#elif WINDOWS
        _ = ExecuteWindowsScriptAsync($"window.tasnimBookGoToPage?.({pageIndex + 1});");
#endif
    }

    private void ApplyZoom(double zoom)
    {
        if (_updatingFromViewer) return;

#if ANDROID || IOS
        _viewer.Zoom = (float)zoom;
#elif WINDOWS
        if (_documentReady)
            _ = ExecuteWindowsScriptAsync($"window.tasnimBookSetZoom?.({zoom.ToString(System.Globalization.CultureInfo.InvariantCulture)});");
#endif
    }

    private void ReportPageChanged(int pageIndex, int pageCount)
    {
        _updatingFromViewer = true;
        CurrentPage = pageIndex;
        _updatingFromViewer = false;
        PageChanged?.Invoke(this, new PdfPageChangedEventArgs(pageIndex, pageCount));
    }

#if WINDOWS
    private async void OnWindowsHandlerChanged(object? sender, EventArgs e)
    {
        if (_viewer.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.WebView2 webView ||
            ReferenceEquals(_platformViewer, webView)) return;

        _platformViewer = webView;
        try
        {
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webView.CoreWebView2.WebMessageReceived += OnWindowsWebMessageReceived;
            webView.NavigationCompleted += OnWindowsNavigationCompleted;
            _webViewInitialized = true;
            await LoadWindowsSourceAsync();
        }
        catch (Exception exception)
        {
            Error?.Invoke(this, exception.Message);
        }
    }

    private async Task LoadWindowsSourceAsync()
    {
        if (!_webViewInitialized || _platformViewer?.CoreWebView2 is null ||
            string.IsNullOrWhiteSpace(SourcePath) || !File.Exists(SourcePath)) return;

        // MAUI assets are copied below the Windows app's base directory using their
        // logical names (pdfjs/...). Mapping that directory keeps the viewer offline.
        var appFolder = AppContext.BaseDirectory;
        _platformViewer.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "tasnimbook-app.invalid", appFolder, CoreWebView2HostResourceAccessKind.Deny);

        // Give the downloaded-book directory its own virtual host. AccessKind.Allow adds
        // the CORS permission PDF.js needs, while exposing only this downloads directory.
        var bookFolder = Path.GetDirectoryName(SourcePath);
        if (string.IsNullOrWhiteSpace(bookFolder)) return;
        _platformViewer.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "tasnimbook-books.invalid", bookFolder, CoreWebView2HostResourceAccessKind.Allow);

        var version = File.GetLastWriteTimeUtc(SourcePath).Ticks;
        var fileName = Uri.EscapeDataString(Path.GetFileName(SourcePath));
        var bookUrl = $"https://tasnimbook-books.invalid/{fileName}?v={version}";
        var viewerUrl = $"https://tasnimbook-app.invalid/pdfjs/web/viewer.html?file={Uri.EscapeDataString(bookUrl)}";
        _documentReady = false;
        _platformViewer.CoreWebView2.Navigate(viewerUrl);
        await Task.CompletedTask;
    }

    private async void OnWindowsNavigationCompleted(
        Microsoft.UI.Xaml.Controls.WebView2 sender,
        CoreWebView2NavigationCompletedEventArgs args)
    {
        if (!args.IsSuccess)
        {
            Error?.Invoke(this, $"PDF.js navigation failed: {args.WebErrorStatus}");
            return;
        }

        const string bridge = """
            (() => {
              const attach = () => {
                const app = window.PDFViewerApplication;
                if (!app?.eventBus) { setTimeout(attach, 50); return; }
                if (window.__tasnimBookBridgeAttached) return;
                window.__tasnimBookBridgeAttached = true;
                window.tasnimBookGoToPage = page => { if (app.pdfViewer) app.pdfViewer.currentPageNumber = page; };
                window.tasnimBookSetZoom = zoom => { if (app.pdfViewer) app.pdfViewer.currentScaleValue = String(zoom); };
                app.eventBus.on('pagesinit', () => {
                  chrome.webview.postMessage(JSON.stringify({ type: 'loaded', pageCount: app.pagesCount || app.pdfViewer?.pagesCount || app.pdfDocument?.numPages || 0 }));
                });
                app.eventBus.on('pagechanging', event => {
                  chrome.webview.postMessage(JSON.stringify({ type: 'page', page: event.pageNumber, pageCount: app.pagesCount || app.pdfViewer?.pagesCount || 0 }));
                });
                app.eventBus.on('scalechanging', event => {
                  chrome.webview.postMessage(JSON.stringify({ type: 'zoom', zoom: event.scale }));
                });
                app.eventBus.on('documenterror', event => {
                  chrome.webview.postMessage(JSON.stringify({ type: 'error', message: event.message || event.reason || 'PDF load failed' }));
                });
              };
              attach();
            })();
            """;
        await ExecuteWindowsScriptAsync(bridge);
    }

    private void OnWindowsWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        try
        {
            using var message = JsonDocument.Parse(args.TryGetWebMessageAsString());
            var root = message.RootElement;
            var type = root.GetProperty("type").GetString();
            var pageCount = root.TryGetProperty("pageCount", out var countElement) ? countElement.GetInt32() : 0;
            var page = root.TryGetProperty("page", out var pageElement) ? pageElement.GetInt32() : 1;
            var zoom = root.TryGetProperty("zoom", out var zoomElement) ? zoomElement.GetDouble() : 1;
            var errorMessage = root.TryGetProperty("message", out var messageElement)
                ? messageElement.GetString()
                : null;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                switch (type)
                {
                    case "loaded":
                        _documentReady = true;
                        DocumentLoaded?.Invoke(this, new PdfDocumentLoadedEventArgs(pageCount));
                        NavigateToPage(CurrentPage);
                        ApplyZoom(Zoom);
                        break;
                    case "page":
                        ReportPageChanged(Math.Max(0, page - 1), pageCount);
                        break;
                    case "zoom":
                        _updatingFromViewer = true;
                        Zoom = zoom;
                        _updatingFromViewer = false;
                        break;
                    case "error":
                        Error?.Invoke(this, errorMessage ?? "PDF load failed");
                        break;
                }
            });
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
        }
    }

    private async Task ExecuteWindowsScriptAsync(string script)
    {
        if (_platformViewer?.CoreWebView2 is not null)
            await _platformViewer.CoreWebView2.ExecuteScriptAsync(script);
    }
#endif
}
