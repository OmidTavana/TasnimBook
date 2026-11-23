using System.ComponentModel.DataAnnotations;
using TasnimBook.Data.Models.DTO;

namespace TasnimBook.Admin.Models;

public sealed class ReleasePageViewModel
{
    public AdminAppReleaseListDto Releases { get; set; } = new();
    public ReleaseFormModel Draft { get; set; } = new();
}

public sealed class ReleaseFormModel
{
    [Range(1, 2)] public int PlatformId { get; set; } = 1;
    [Required, StringLength(32)] public string Version { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int Build { get; set; }
    [Required, StringLength(4000)] public string ReleaseNotes { get; set; } = string.Empty;
    [Required] public IFormFile? File { get; set; }
    public bool Publish { get; set; } = true;
    public bool IsMandatory { get; set; }
}
