using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using TasnimBook.Data;
using TasnimBook.Data.Interfaces;
using TasnimBook.Data.Models.DTO;
using webapicore.Fillter;

namespace webapicore.Services;

public interface IAccountTokenService
{
    Task<AccountAuthResult> CreateSessionAsync(AppUser user, string? deviceName);
    Task<AccountAuthResult?> RefreshSessionAsync(string refreshToken, string? deviceName);
    Task RevokeAsync(string refreshToken);
}

public sealed class AccountTokenService : IAccountTokenService
{
    private readonly IUserAccountRepository _accounts;
    private readonly JwtConfig _config;

    public AccountTokenService(IUserAccountRepository accounts, Microsoft.Extensions.Options.IOptions<JwtConfig> config)
    {
        _accounts = accounts;
        _config = config.Value;
    }

    public async Task<AccountAuthResult> CreateSessionAsync(AppUser user, string? deviceName)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(Math.Clamp(_config.AccessTokenMinutes, 5, 120));
        var jwtId = Guid.NewGuid().ToString("N");
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(JwtRegisteredClaimNames.Jti, jwtId),
            new Claim("token_kind", "account")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.Secret));
        var token = new JwtSecurityToken(
            issuer: _config.Issuer,
            audience: _config.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var rawRefreshToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        await _accounts.AddRefreshTokenAsync(new UserRefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawRefreshToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(Math.Clamp(_config.RefreshTokenDays, 1, 90)),
            DeviceName = string.IsNullOrWhiteSpace(deviceName) ? null : deviceName.Trim()
        });

        return new AccountAuthResult
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            RefreshToken = rawRefreshToken,
            ExpiresAtUtc = expires,
            User = ToProfile(user)
        };
    }

    public async Task<AccountAuthResult?> RefreshSessionAsync(string refreshToken, string? deviceName)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return null;
        var oldToken = await _accounts.FindRefreshTokenAsync(HashToken(refreshToken));
        if (oldToken is null || oldToken.RevokedAtUtc is not null || oldToken.ExpiresAtUtc <= DateTime.UtcNow || !oldToken.User.IsActive)
            return null;

        oldToken.RevokedAtUtc = DateTime.UtcNow;
        var result = await CreateSessionAsync(oldToken.User, deviceName ?? oldToken.DeviceName);
        oldToken.ReplacedByTokenHash = HashToken(result.RefreshToken);
        await _accounts.SaveChangesAsync();
        return result;
    }

    public async Task RevokeAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;
        var token = await _accounts.FindRefreshTokenAsync(HashToken(refreshToken));
        if (token is null || token.RevokedAtUtc is not null) return;
        token.RevokedAtUtc = DateTime.UtcNow;
        await _accounts.SaveChangesAsync();
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static AccountProfileDto ToProfile(AppUser user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        DisplayName = user.DisplayName
    };
}
