using System.ComponentModel.DataAnnotations;

namespace TasnimBook.Data.Models.DTO;

public sealed class RegisterAccountRequest
{
    [Required, EmailAddress, MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(2), MaxLength(80)]
    public string DisplayName { get; set; } = string.Empty;

    [Required, MinLength(10), MaxLength(128)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? DeviceName { get; set; }
}

public sealed class LoginAccountRequest
{
    [Required, EmailAddress, MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? DeviceName { get; set; }
}

public sealed class RefreshAccountRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? DeviceName { get; set; }
}

public sealed class AccountProfileDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class AccountAuthResult
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public AccountProfileDto User { get; set; } = new();
}

public sealed class ReadingProgressDto
{
    [Range(0, 100)]
    public decimal PositionPercent { get; set; }

    [Range(0, int.MaxValue)]
    public int? CurrentPage { get; set; }

    [Range(0, int.MaxValue)]
    public int? TotalPages { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class FavoriteStateDto
{
    public int BookId { get; set; }
    public bool IsFavorite { get; set; }
}
