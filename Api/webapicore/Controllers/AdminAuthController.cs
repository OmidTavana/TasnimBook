using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TasnimBook.Data;
using TasnimBook.Data.DataSource;
using TasnimBook.Data.Interfaces;
using TasnimBook.Data.Models.DTO;
using webapicore.Fillter;
using webapicore.Services;

namespace webapicore.Controllers;

[ApiController, ApiKeyAuth, Route("api/[controller]/[action]")]
public sealed class AdminAuthController(
    DbContextAnti db,
    IUserAccountRepository accounts,
    IPasswordHasher<AppUser> passwordHasher,
    IAccountTokenService tokens) : ControllerBase
{
    [AllowAnonymous, HttpPost]
    public async Task<ActionResult<AccountAuthResult>> Login(LoginAccountRequest request)
    {
        var user = await accounts.FindByNormalizedEmailAsync(request.Email.Trim().ToUpperInvariant());
        if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "ایمیل یا رمز عبور نادرست است." });
        if (user.AdminStatus is AdminApprovalStatuses.PendingApproval or AdminApprovalStatuses.Rejected)
            return Ok(new AccountAuthResult { User = new AccountProfileDto { Id = user.Id, Email = user.Email, DisplayName = user.DisplayName, Role = user.Role, AdminStatus = user.AdminStatus } });
        if (!user.IsActive || user.Role is not (UserRoles.ContentAdmin or UserRoles.SuperAdmin))
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = "این حساب دسترسی مدیریتی فعال ندارد." });
        }
        return Ok(await tokens.CreateSessionAsync(user, request.DeviceName));
    }

    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> Register(AdminRegisterRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        if (await db.AppUsers.AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken))
            return Conflict(new { message = "برای این ایمیل قبلاً حساب ساخته شده است." });
        if (request.Password.Length < 10 || !request.Password.Any(char.IsLetter) || !request.Password.Any(char.IsDigit))
            return BadRequest(new { message = "رمز عبور باید حداقل ۱۰ کاراکتر و شامل حرف و عدد باشد." });

        var user = new AppUser
        {
            Email = request.Email.Trim(), NormalizedEmail = normalizedEmail, DisplayName = request.DisplayName.Trim(),
            CreatedAtUtc = DateTime.UtcNow, IsActive = true, Role = UserRoles.User,
            AdminStatus = AdminApprovalStatuses.PendingApproval,
            AdminReviewNote = string.IsNullOrWhiteSpace(request.ApplicationNote) ? null : request.ApplicationNote.Trim()
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        await accounts.AddUserAsync(user);
        return Accepted(new { user.Id, user.AdminStatus });
    }

    [Authorize(Roles = UserRoles.SuperAdmin), HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminUserDto>>> Pending(CancellationToken cancellationToken) =>
        Ok(await db.AppUsers.AsNoTracking().Where(user => user.AdminStatus == AdminApprovalStatuses.PendingApproval)
            .OrderBy(user => user.CreatedAtUtc).Select(user => new AdminUserDto { Id = user.Id, Email = user.Email, DisplayName = user.DisplayName, Role = user.Role, AdminStatus = user.AdminStatus, IsActive = user.IsActive, CreatedAtUtc = user.CreatedAtUtc }).ToListAsync(cancellationToken));

    [Authorize(Roles = UserRoles.SuperAdmin), HttpPut("{id:int}")]
    public async Task<IActionResult> Review(int id, AdminReviewRequest request, CancellationToken cancellationToken)
    {
        var applicant = await db.AppUsers.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
        if (applicant is null) return NotFound();
        if (applicant.AdminStatus != AdminApprovalStatuses.PendingApproval)
            return Conflict(new { message = "این درخواست قبلاً بررسی شده است." });

        if (request.Approve && request.Role is not (UserRoles.ContentAdmin or UserRoles.SuperAdmin))
            return BadRequest(new { message = "نقش مدیریتی نامعتبر است." });

        applicant.AdminStatus = request.Approve ? AdminApprovalStatuses.Approved : AdminApprovalStatuses.Rejected;
        applicant.Role = request.Approve ? request.Role : UserRoles.User;
        applicant.IsActive = request.Approve;
        applicant.AdminReviewNote = request.Note?.Trim();
        applicant.AdminReviewedAtUtc = DateTime.UtcNow;
        applicant.AdminReviewedByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = UserRoles.SuperAdmin), HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminUserDto>>> Administrators(CancellationToken cancellationToken) =>
        Ok(await db.AppUsers.AsNoTracking().Where(user => user.Role != UserRoles.User || user.AdminStatus != AdminApprovalStatuses.NotApplicable)
            .OrderBy(user => user.DisplayName).Select(user => new AdminUserDto { Id = user.Id, Email = user.Email, DisplayName = user.DisplayName, Role = user.Role, AdminStatus = user.AdminStatus, IsActive = user.IsActive, CreatedAtUtc = user.CreatedAtUtc }).ToListAsync(cancellationToken));

    [Authorize(Roles = UserRoles.SuperAdmin), HttpPut("{id:int}/{active:bool}")]
    public async Task<IActionResult> SetActive(int id, bool active, CancellationToken cancellationToken)
    {
        var user = await db.AppUsers.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null) return NotFound();
        if (!active && user.Role == UserRoles.SuperAdmin && user.IsActive &&
            await db.AppUsers.CountAsync(item => item.Role == UserRoles.SuperAdmin && item.IsActive, cancellationToken) <= 1)
            return Conflict(new { message = "آخرین SuperAdmin فعال را نمی‌توان غیرفعال کرد." });
        user.IsActive = active;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = UserRoles.SuperAdmin), HttpPut("{id:int}/{role}")]
    public async Task<IActionResult> SetRole(int id, string role, CancellationToken cancellationToken)
    {
        if (role is not (UserRoles.ContentAdmin or UserRoles.SuperAdmin)) return BadRequest();
        var user = await db.AppUsers.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null) return NotFound();
        if (user.Role == UserRoles.SuperAdmin && role != UserRoles.SuperAdmin && user.IsActive &&
            await db.AppUsers.CountAsync(item => item.Role == UserRoles.SuperAdmin && item.IsActive, cancellationToken) <= 1)
            return Conflict(new { message = "نقش آخرین SuperAdmin فعال را نمی‌توان تغییر داد." });
        user.Role = role;
        user.AdminStatus = AdminApprovalStatuses.Approved;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

}
