namespace TasnimBook.Data.Models.DTO;

public sealed class AppReleaseDto
{
    public int Id { get; set; }
    public string Platform { get; set; } = string.Empty;
    public int PlatformId { get; set; }
    public string Version { get; set; } = string.Empty;
    public int Build { get; set; }
    public string ReleaseNotes { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public long? FileSize { get; set; }
    public string? Sha256 { get; set; }
    public string? ContentType { get; set; }
    public bool IsMandatory { get; set; }
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; }
    public DateTime PublishedAt { get; set; }
}

public sealed class AppUpdateCheckDto
{
    public bool UpdateAvailable { get; set; }
    public string? LatestVersion { get; set; }
    public int? LatestBuild { get; set; }
    public string? ReleaseNotes { get; set; }
    public string? DownloadUrl { get; set; }
    public long? FileSize { get; set; }
    public string? Sha256 { get; set; }
    public bool IsMandatory { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public sealed class AdminAppReleaseListDto
{
    public AppReleaseDto? LatestAndroid { get; set; }
    public AppReleaseDto? LatestWindows { get; set; }
    public IReadOnlyList<AppReleaseDto> History { get; set; } = [];
}
