using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TasnimBook.Data;
using TasnimBook.Data.Interfaces;
using TasnimBook.Data.Models.DTO;
using webapicore.Fillter;
using webapicore.Services;

namespace webapicore.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
[ApiKeyAuth]
public sealed class AccountController : ControllerBase
{
    private readonly IUserAccountRepository _accounts;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly IAccountTokenService _tokens;

    public AccountController(
        IUserAccountRepository accounts,
        IPasswordHasher<AppUser> passwordHasher,
        IAccountTokenService tokens)
    {
        _accounts = accounts;
        _passwordHasher = passwordHasher;
        _tokens = tokens;
    }

    [HttpPost]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AccountAuthResult>> Register(RegisterAccountRequest request)
    {
        if (!IsStrongPassword(request.Password))
            return BadRequest(new { message = "رمز عبور باید حداقل ۱۰ کاراکتر و شامل حرف و عدد باشد." });

        var normalizedEmail = NormalizeEmail(request.Email);
        if (await _accounts.FindByNormalizedEmailAsync(normalizedEmail) is not null)
            return Conflict(new { message = "برای این ایمیل قبلاً حساب ساخته شده است." });

        var user = new AppUser
        {
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            DisplayName = request.DisplayName.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        await _accounts.AddUserAsync(user);
        return Ok(await _tokens.CreateSessionAsync(user, request.DeviceName));
    }

    [HttpPost]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AccountAuthResult>> Login(LoginAccountRequest request)
    {
        var user = await _accounts.FindByNormalizedEmailAsync(NormalizeEmail(request.Email));
        if (user is null || !user.IsActive)
            return Unauthorized(new { message = "ایمیل یا رمز عبور نادرست است." });

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "ایمیل یا رمز عبور نادرست است." });

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        user.LastLoginAtUtc = DateTime.UtcNow;
        await _accounts.SaveChangesAsync();
        return Ok(await _tokens.CreateSessionAsync(user, request.DeviceName));
    }

    [HttpPost]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AccountAuthResult>> Refresh(RefreshAccountRequest request)
    {
        var result = await _tokens.RefreshSessionAsync(request.RefreshToken, request.DeviceName);
        return result is null ? Unauthorized(new { message = "نشست کاربری منقضی یا نامعتبر است." }) : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Logout(RefreshAccountRequest request)
    {
        await _tokens.RevokeAsync(request.RefreshToken);
        return NoContent();
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<AccountProfileDto>> Profile()
    {
        var user = await _accounts.FindByIdAsync(GetUserId());
        if (user is null) return NotFound();
        return Ok(new AccountProfileDto { Id = user.Id, Email = user.Email, DisplayName = user.DisplayName });
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult> Favorites() => Ok(await _accounts.GetFavoritesAsync(GetUserId()));

    [HttpGet("{bookId:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<FavoriteStateDto>> FavoriteState(int bookId) =>
        Ok(new FavoriteStateDto { BookId = bookId, IsFavorite = await _accounts.IsFavoriteAsync(GetUserId(), bookId) });

    [HttpPut("{bookId:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<FavoriteStateDto>> Favorite(int bookId, FavoriteStateDto request)
    {
        try
        {
            await _accounts.SetFavoriteAsync(GetUserId(), bookId, request.IsFavorite);
            return Ok(new FavoriteStateDto { BookId = bookId, IsFavorite = request.IsFavorite });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("{bookId:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<ReadingProgressDto>> Progress(int bookId)
    {
        var progress = await _accounts.GetProgressAsync(GetUserId(), bookId);
        if (progress is null) return Ok(new ReadingProgressDto());
        return Ok(new ReadingProgressDto
        {
            PositionPercent = progress.PositionPercent,
            CurrentPage = progress.CurrentPage,
            TotalPages = progress.TotalPages,
            UpdatedAtUtc = progress.UpdatedAtUtc
        });
    }

    [HttpPut("{bookId:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Progress(int bookId, ReadingProgressDto request)
    {
        try
        {
            await _accounts.UpsertProgressAsync(GetUserId(), bookId, request);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private int GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(value, out var userId) ? userId : throw new UnauthorizedAccessException();
    }

    private static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    private static bool IsStrongPassword(string password) =>
        password.Length >= 10 && password.Any(char.IsLetter) && password.Any(char.IsDigit);
}
