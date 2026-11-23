using TasnimBook.Data;

namespace TasnimBook.MAUI.Services;

public enum AppUpdateState
{
    Idle,
    Checking,
    Available,
    Downloading,
    Downloaded,
    Installing,
    UpToDate,
    Failed,
    Unsupported
}

public sealed class AppUpdateService
{
    private readonly ApiSessionService _session;
    private readonly AppNavigationService _navigation;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private bool _automaticCheckCompleted;
    private AppVersion? _release;
    private string? _downloadedPath;
    private AppUpdateState _state = AppUpdateState.Idle;
    private double _downloadProgress;
    private string _statusMessage = "برای بررسی نسخه جدید آماده است.";

    public AppUpdateService(ApiSessionService session, AppNavigationService navigation)
    {
        _session = session;
        _navigation = navigation;
    }

    public event EventHandler? StateChanged;

    public AppUpdateState State => _state;
    public double DownloadProgress => _downloadProgress;
    public string StatusMessage => _statusMessage;
    public string CurrentVersion => $"{AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})";
    public string? AvailableVersion => _release is null
        ? null
        : $"{_release.VersionName} ({_release.VersionNumber})";
    public string? ReleaseNotes => _release?.ReleaseNotes;
    public bool CanDownload => _state == AppUpdateState.Available;
    public bool CanInstall => _state == AppUpdateState.Downloaded && !string.IsNullOrWhiteSpace(_downloadedPath);
    public bool IsBusy => _state is AppUpdateState.Checking or AppUpdateState.Downloading or AppUpdateState.Installing;

    public async Task CheckForUpdateAsync(bool force = false)
    {
        if (!IsSupportedPlatform())
        {
            SetState(AppUpdateState.Unsupported, "بررسی و نصب مستقیم برای این سیستم‌عامل فعال نیست.");
            return;
        }

        if (!force && _automaticCheckCompleted) return;
        if (!await _operationGate.WaitAsync(0)) return;

        var shouldOffer = false;
        try
        {
            SetState(AppUpdateState.Checking, "در حال بررسی آخرین نسخه…");
            var repository = await _session.GetRepositoryAsync();
            var release = await repository.IAppVersions.GetAppVersion(GetPlatformId());
            var currentBuild = CurrentBuildNumber();
            _automaticCheckCompleted = true;

            if (release is null || !AppVersionOrdering.IsNewer(release, AppInfo.Current.VersionString, currentBuild) ||
                !TryResolveDownloadUri(release.Url, out _))
            {
                _release = null;
                _downloadedPath = null;
                _downloadProgress = 0;
                SetState(AppUpdateState.UpToDate, "شما از آخرین نسخه برنامه استفاده می‌کنید.");
                return;
            }

            _release = release;
            _downloadProgress = 0;
            _downloadedPath = BuildPackagePath(release);
            if (await IsPackageValidAsync(_downloadedPath, release.Sha256))
            {
                _downloadProgress = 1;
                SetState(AppUpdateState.Downloaded, $"نسخه {release.VersionName} قبلاً دریافت شده و آماده نصب است.");
            }
            else
            {
                _downloadedPath = null;
                SetState(AppUpdateState.Available, $"نسخه {release.VersionName} برای دریافت آماده است.");
            }

            shouldOffer = !force;
        }
        catch (Exception exception)
        {
            _automaticCheckCompleted = true;
            SetState(AppUpdateState.Failed, $"بررسی نسخه جدید ناموفق بود: {exception.Message}");
        }
        finally
        {
            _operationGate.Release();
        }

        if (shouldOffer)
            await OfferCurrentUpdateAsync();
    }

    public async Task DownloadUpdateAsync()
    {
        if (_release is null || !TryResolveDownloadUri(_release.Url, out var downloadUri))
        {
            SetState(AppUpdateState.Failed, "آدرس فایل نسخه جدید معتبر نیست.");
            return;
        }

        if (!await _operationGate.WaitAsync(0)) return;
        try
        {
            var finalPath = BuildPackagePath(_release);
            if (await IsPackageValidAsync(finalPath, _release.Sha256))
            {
                _downloadedPath = finalPath;
                _downloadProgress = 1;
                SetState(AppUpdateState.Downloaded, "فایل به‌روزرسانی آماده نصب است.");
                return;
            }

            SetState(AppUpdateState.Downloading, "در حال دریافت فایل به‌روزرسانی…");
            _downloadProgress = 0;
            NotifyChanged();
            _downloadedPath = await DownloadPackageAsync(downloadUri, finalPath);
            _downloadProgress = 1;
            SetState(AppUpdateState.Downloaded, "دانلود کامل شد؛ اکنون می‌توانید نسخه جدید را نصب کنید.");
        }
        catch (Exception exception)
        {
            _downloadedPath = null;
            SetState(AppUpdateState.Failed, $"دانلود نسخه جدید ناموفق بود: {exception.Message}");
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async Task InstallUpdateAsync()
    {
        if (string.IsNullOrWhiteSpace(_downloadedPath) || !await IsPackageValidAsync(_downloadedPath, _release?.Sha256))
        {
            _downloadedPath = null;
            SetState(AppUpdateState.Failed, "فایل دانلودشده معتبر نیست؛ دوباره آن را دریافت کنید.");
            return;
        }

        if (!await _operationGate.WaitAsync(0)) return;
        try
        {
            SetState(AppUpdateState.Installing, "در حال بازکردن نصب‌کننده…");
            var opened = await OpenInstallerAsync(_downloadedPath);
            var canRetryAfterPermission = !opened && DeviceInfo.Platform == DevicePlatform.Android;
            SetState(
                opened || canRetryAfterPermission ? AppUpdateState.Downloaded : AppUpdateState.Failed,
                opened
                    ? "نصب‌کننده باز شد؛ مراحل نصب را در سیستم‌عامل ادامه دهید."
                    : canRetryAfterPermission
                        ? "پس از فعال‌کردن اجازه نصب از این منبع، دوباره دکمه نصب را بزنید."
                        : "بازکردن نصب‌کننده امکان‌پذیر نبود.");
        }
        catch (Exception exception)
        {
            SetState(AppUpdateState.Failed, $"شروع نصب ناموفق بود: {exception.Message}");
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async Task HandleNotificationAsync(AppNotification notification)
    {
        if (!IsSupportedPlatform())
        {
            await _navigation.AlertAsync(
                notification.Title,
                notification.Message,
                type: AppMessageType.Information);
            return;
        }

        if (notification.VersionNumber is not { } versionNumber || versionNumber <= CurrentBuildNumber())
            return;

        if (!TryResolveDownloadUri(notification.ActionUrl, out _))
        {
            await CheckForUpdateAsync(force: true);
            if (_state is AppUpdateState.Available or AppUpdateState.Downloaded)
                await OfferCurrentUpdateAsync(notification.Title, notification.Message);
            return;
        }

        _release = new AppVersion
        {
            VersionName = notification.VersionName ?? versionNumber.ToString(),
            VersionNumber = versionNumber,
            ReleaseNotes = notification.Message,
            Url = notification.ActionUrl!,
            PlatformId = GetPlatformId(),
            ReleaseDate = notification.PublishAtUtc
        };
        _downloadedPath = null;
        _downloadProgress = 0;
        SetState(AppUpdateState.Available, $"نسخه {_release.VersionName} برای دریافت آماده است.");
        await OfferCurrentUpdateAsync(notification.Title, notification.Message);
    }

    private async Task OfferCurrentUpdateAsync(string? title = null, string? message = null)
    {
        if (_release is null) return;
        var accepted = await _navigation.ConfirmAsync(
            title ?? $"نسخه {_release.VersionName} آماده است",
            message ?? (string.IsNullOrWhiteSpace(_release.ReleaseNotes)
                ? "نسخه تازه‌ای از تسنیم‌بوک آماده دریافت است."
                : _release.ReleaseNotes),
            _state == AppUpdateState.Downloaded ? "نصب" : "دانلود",
            "بعداً",
            AppMessageType.Information);
        if (!accepted) return;

        if (_state != AppUpdateState.Downloaded)
            await DownloadUpdateAsync();
        if (_state == AppUpdateState.Downloaded)
            await InstallUpdateAsync();
    }

    private async Task<string> DownloadPackageAsync(Uri uri, string finalPath)
    {
        var temporaryPath = finalPath + ".download";
        if (File.Exists(temporaryPath)) File.Delete(temporaryPath);

        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(20) };
        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        var total = response.Content.Headers.ContentLength;

        await using var source = await response.Content.ReadAsStreamAsync();
        await using var destination = new FileStream(
            temporaryPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            64 * 1024,
            useAsync: true);
        var buffer = new byte[64 * 1024];
        long received = 0;
        int read;
        while ((read = await source.ReadAsync(buffer)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, read));
            received += read;
            if (total is > 0)
            {
                _downloadProgress = Math.Clamp((double)received / total.Value, 0, 1);
                _statusMessage = $"در حال دانلود… {Math.Round(_downloadProgress * 100):0}٪";
                NotifyChanged();
            }
        }
        await destination.FlushAsync();

        if (!await IsPackageValidAsync(temporaryPath, _release?.Sha256))
        {
            File.Delete(temporaryPath);
            throw new InvalidDataException("ساختار فایل دریافت‌شده معتبر نیست.");
        }

        File.Move(temporaryPath, finalPath, true);
        return finalPath;
    }

    private string BuildPackagePath(AppVersion release)
    {
        var extension = ".bin";
        if (TryResolveDownloadUri(release.Url, out var uri))
            extension = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();

#if ANDROID
        extension = ".apk";
#elif WINDOWS
        if (extension is not (".msix" or ".msixbundle" or ".appx" or ".appxbundle" or ".exe"))
            extension = ".bin";
#endif
        return Path.Combine(FileSystem.CacheDirectory, $"tasnimbook-{release.VersionNumber}{extension}");
    }

    private static async Task<bool> IsPackageValidAsync(string? path, string? expectedSha256 = null)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
        var info = new FileInfo(path);
        if (info.Length < 4) return false;

        var header = new byte[4];
        await using var stream = File.OpenRead(path);
        if (await stream.ReadAsync(header) != header.Length) return false;
        var isZip = header[0] == 0x50 && header[1] == 0x4B &&
                    header[2] is 0x03 or 0x05 or 0x07 &&
                    header[3] is 0x04 or 0x06 or 0x08;

        if (!string.IsNullOrWhiteSpace(expectedSha256))
        {
            stream.Position = 0;
            var actualHash = Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(stream)).ToLowerInvariant();
            if (!string.Equals(actualHash, expectedSha256.Trim(), StringComparison.OrdinalIgnoreCase)) return false;
        }

#if ANDROID
        return isZip;
#elif WINDOWS
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension == ".exe"
            ? header[0] == 0x4D && header[1] == 0x5A
            : (extension is ".msix" or ".msixbundle" or ".appx" or ".appxbundle") && isZip;
#else
        return false;
#endif
    }

    private static Task<bool> OpenInstallerAsync(string packagePath)
    {
#if ANDROID
        return AndroidApkInstaller.InstallAsync(packagePath);
#elif WINDOWS
        return Launcher.Default.OpenAsync(new OpenFileRequest(
            "نصب به‌روزرسانی تسنیم‌بوک",
            new ReadOnlyFile(packagePath)));
#else
        return Task.FromResult(false);
#endif
    }

    private static bool TryResolveDownloadUri(string? value, out Uri uri) =>
        ApiConfiguration.TryResolveResourceUri(value, out uri);

    private static bool IsSupportedPlatform() =>
        DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.WinUI;

    private static int GetPlatformId() => DeviceInfo.Platform == DevicePlatform.WinUI
        ? (int)TasnimBook.Data.Platforms.Windows
        : (int)TasnimBook.Data.Platforms.Android;

    private static int CurrentBuildNumber() =>
        int.TryParse(AppInfo.Current.BuildString, out var build) ? build : 0;

    private void SetState(AppUpdateState state, string message)
    {
        _state = state;
        _statusMessage = message;
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        if (MainThread.IsMainThread)
            StateChanged?.Invoke(this, EventArgs.Empty);
        else
            MainThread.BeginInvokeOnMainThread(() => StateChanged?.Invoke(this, EventArgs.Empty));
    }
}

#if ANDROID
internal static class AndroidApkInstaller
{
    public static Task<bool> InstallAsync(string apkPath)
    {
        var context = Android.App.Application.Context;

        if (OperatingSystem.IsAndroidVersionAtLeast(26) &&
            !context.PackageManager!.CanRequestPackageInstalls())
        {
            var settingsIntent = new Android.Content.Intent(
                Android.Provider.Settings.ActionManageUnknownAppSources,
                Android.Net.Uri.Parse($"package:{context.PackageName}"));
            settingsIntent.AddFlags(Android.Content.ActivityFlags.NewTask);
            context.StartActivity(settingsIntent);
            Android.Widget.Toast.MakeText(
                context,
                "اجازه نصب از این برنامه را فعال کنید و سپس دوباره دکمه نصب را بزنید.",
                Android.Widget.ToastLength.Long)!.Show();
            return Task.FromResult(false);
        }

        var file = new Java.IO.File(apkPath);
        var contentUri = AndroidX.Core.Content.FileProvider.GetUriForFile(
            context,
            $"{context.PackageName}.fileprovider",
            file);
        var installIntent = new Android.Content.Intent(Android.Content.Intent.ActionView);
        installIntent.SetDataAndType(contentUri, "application/vnd.android.package-archive");
        installIntent.AddFlags(Android.Content.ActivityFlags.NewTask |
                               Android.Content.ActivityFlags.GrantReadUriPermission);
        context.StartActivity(installIntent);
        return Task.FromResult(true);
    }
}
#endif
