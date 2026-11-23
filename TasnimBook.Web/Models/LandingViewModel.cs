using TasnimBook.Data.Models.DTO;

namespace TasnimBook.Web.Models;

public sealed class LandingViewModel
{
    public AppReleaseDto? Android { get; set; }
    public AppReleaseDto? Windows { get; set; }
    public bool BackendUnavailable { get; set; }
    public string? CanonicalUrl { get; set; }
}
