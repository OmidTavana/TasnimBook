using TasnimBook.Data;

namespace TasnimBook.MAUI.Services;

public sealed class AppUpdateService
{
    private readonly ApiSessionService _session;
    private readonly AppNavigationService _navigation;
    private bool _isChecking;
    private bool _checkCompleted;

    public AppUpdateService(ApiSessionService session, AppNavigationService navigation)
    {
        _session = session;
        _navigation = navigation;
    }

    public async Task CheckForUpdateAsync()
    {
#if !ANDROID
        await Task.CompletedTask;
        return;
#else
        if (_isChecking || _checkCompleted) return;
        _isChecking = true;

        try
        {
            var repository = await _session.GetRepositoryAsync();
            var release = await repository.IAppVersions.GetAppVersion((int)TasnimBook.Data.Platforms.Android);
            var currentBuild = int.TryParse(AppInfo.Current.BuildString, out var build) ? build : 0;

            if (release is null || release.VersionNumber <= currentBuild ||
                !TryResolveDownloadUri(release.Url, out var downloadUri))
            {
                _checkCompleted = true;
                return;
            }

            var notes = string.IsNullOrWhiteSpace(release.ReleaseNotes)
                ? "نسخه تازه‌ای از تسنیم‌بوک آماده است."
                : release.ReleaseNotes;
            var accepted = await _navigation.ConfirmAsync(
                $"نسخه {release.VersionName} آماده است",
                $"{notes}\n\nفایل APK دانلود و برای نصب باز شود؟",
                "دانلود و نصب",
                "بعداً");

            if (!accepted)
            {
                _checkCompleted = true;
                return;
            }

            var apkPath = await DownloadApkAsync(downloadUri, release.VersionNumber);
            _checkCompleted = await AndroidApkInstaller.InstallAsync(apkPath);
        }
        catch (Exception exception)
        {
            _checkCompleted = true;
            await _navigation.AlertAsync("به‌روزرسانی برنامه", $"دریافت یا نصب نسخه جدید انجام نشد.\n{exception.Message}");
        }
        finally
        {
            _isChecking = false;
        }
#endif
    }

    private static bool TryResolveDownloadUri(string? value, out Uri uri)
    {
        return ApiConfiguration.TryResolveResourceUri(value, out uri);
    }

    private static async Task<string> DownloadApkAsync(Uri uri, int versionNumber)
    {
        var path = Path.Combine(FileSystem.CacheDirectory, $"tasnimbook-{versionNumber}.apk");
        if (await IsApkAsync(path)) return path;

        var temporaryPath = path + ".download";
        if (File.Exists(temporaryPath)) File.Delete(temporaryPath);

        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using (var source = await response.Content.ReadAsStreamAsync())
        await using (var destination = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, true))
        {
            await source.CopyToAsync(destination);
        }

        if (!await IsApkAsync(temporaryPath))
        {
            File.Delete(temporaryPath);
            throw new InvalidDataException("فایل دریافت‌شده APK معتبر نیست.");
        }

        File.Move(temporaryPath, path, true);
        return path;
    }

    private static async Task<bool> IsApkAsync(string path)
    {
        if (!File.Exists(path)) return false;
        var header = new byte[4];
        await using var stream = File.OpenRead(path);
        var read = await stream.ReadAsync(header);
        return read == 4 && header[0] == 0x50 && header[1] == 0x4B &&
               header[2] is 0x03 or 0x05 or 0x07 && header[3] is 0x04 or 0x06 or 0x08;
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
                "اجازه نصب از این برنامه را فعال کنید و سپس به تسنیم‌بوک برگردید.",
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
