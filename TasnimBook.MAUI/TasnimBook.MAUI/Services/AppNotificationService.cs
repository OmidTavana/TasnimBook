using System.Text.Json;
using TasnimBook.Data;

namespace TasnimBook.MAUI.Services;

public sealed class AppNotificationService
{
    private const string SeenKeyPrefix = "TasnimBook.Notification.Seen.";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AppNavigationService _navigation;
    private readonly AppUpdateService _updates;
    private bool _isChecking;

    public AppNotificationService(AppNavigationService navigation, AppUpdateService updates)
    {
        _navigation = navigation;
        _updates = updates;
    }

    public async Task CheckAsync(CancellationToken cancellationToken = default)
    {
        if (_isChecking) return;
        _isChecking = true;
        try
        {
            var currentBuild = int.TryParse(AppInfo.Current.BuildString, out var build) ? build : 0;
            using var client = new HttpClient
            {
                BaseAddress = new Uri(ApiConfiguration.BaseAddress),
                Timeout = TimeSpan.FromSeconds(30)
            };
            client.DefaultRequestHeaders.Add("APIKey", ApiConfiguration.ApiKey);
            var platformId = GetPlatformId();
            using var response = await client.GetAsync(
                $"AppNotifications/Active?platformId={platformId}&currentVersion={currentBuild}",
                cancellationToken);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var notifications = JsonSerializer.Deserialize<List<AppNotification>>(content, JsonOptions) ?? [];

            foreach (var notification in notifications.Where(item => !IsAcknowledged(item.Id)))
            {
                var acknowledged = true;
                if (notification.NotificationType == AppNotificationTypes.AppUpdate)
                    await _updates.HandleNotificationAsync(notification);
                else if (TryGetToastType(notification.NotificationType, out var toastType))
                    await _navigation.ToastAsync(notification.Message, toastType);
                else
                    acknowledged = await _navigation.AcknowledgeAsync(
                        notification.Title,
                        notification.Message,
                        type: AppMessageType.Information);

                // Persist only after the user explicitly acknowledges the important
                // dialog (or finishes the update prompt). Back-dismissed and failed
                // displays are offered again on the next check.
                if (acknowledged)
                    MarkAsAcknowledged(notification.Id);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch
        {
            // Notifications must never prevent the home page from loading.
        }
        finally
        {
            _isChecking = false;
        }
    }

    private static bool IsAcknowledged(int notificationId) =>
        Preferences.Default.Get(SeenKeyPrefix + notificationId, false);

    private static void MarkAsAcknowledged(int notificationId) =>
        Preferences.Default.Set(SeenKeyPrefix + notificationId, true);

    private static bool TryGetToastType(string notificationType, out AppMessageType type)
    {
        type = notificationType switch
        {
            AppNotificationTypes.Success => AppMessageType.Success,
            AppNotificationTypes.Error => AppMessageType.Error,
            AppNotificationTypes.Warning => AppMessageType.Warning,
            AppNotificationTypes.Information => AppMessageType.Information,
            _ => AppMessageType.Information
        };
        return notificationType is AppNotificationTypes.Success or AppNotificationTypes.Error or
            AppNotificationTypes.Warning or AppNotificationTypes.Information;
    }

    private static int GetPlatformId()
    {
        if (DeviceInfo.Platform == DevicePlatform.Android) return (int)TasnimBook.Data.Platforms.Android;
        if (DeviceInfo.Platform == DevicePlatform.WinUI) return (int)TasnimBook.Data.Platforms.Windows;
        if (DeviceInfo.Platform == DevicePlatform.iOS) return (int)TasnimBook.Data.Platforms.Ios;
        return 0;
    }
}
