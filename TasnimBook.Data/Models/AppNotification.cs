using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TasnimBook.Data;

[Table("appnotification")]
public sealed class AppNotification
{
    [Column("id")]
    public int Id { get; set; }

    [Required, MaxLength(120), Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(2000), Column("message")]
    public string Message { get; set; } = string.Empty;

    [Required, MaxLength(32), Column("notificationtype")]
    public string NotificationType { get; set; } = AppNotificationTypes.General;

    [MaxLength(1000), Column("actionurl")]
    public string? ActionUrl { get; set; }

    [MaxLength(32), Column("versionname")]
    public string? VersionName { get; set; }

    [Column("versionnumber")]
    public int? VersionNumber { get; set; }

    [Column("platformid")]
    public int? PlatformId { get; set; }

    [Column("publishatutc")]
    public DateTime PublishAtUtc { get; set; } = DateTime.UtcNow;

    [Column("expiresatutc")]
    public DateTime? ExpiresAtUtc { get; set; }

    [Column("isactive")]
    public bool IsActive { get; set; } = true;

    [Column("createdatutc")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public static class AppNotificationTypes
{
    public const string General = "General";
    public const string AppUpdate = "AppUpdate";
    public const string Success = "Success";
    public const string Error = "Error";
    public const string Warning = "Warning";
    public const string Information = "Information";
}
