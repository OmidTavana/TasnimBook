using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TasnimBook.Data;
using TasnimBook.Data.DataSource;

namespace webapicore.Services;

public sealed class SuperAdminProvisioner(IServiceProvider services, IConfiguration configuration, ILogger<SuperAdminProvisioner> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var email = configuration["BootstrapSuperAdmin:Email"]?.Trim();
        var password = configuration["BootstrapSuperAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;
        if (password.Length < 14 || !password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            throw new InvalidOperationException("Bootstrap SuperAdmin password must contain letters and digits and be at least 14 characters.");

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DbContextAnti>();
        if (await db.AppUsers.AnyAsync(user => user.Role == UserRoles.SuperAdmin && user.IsActive, cancellationToken))
        {
            logger.LogInformation("An active SuperAdmin already exists; bootstrap settings were ignored.");
            return;
        }

        var normalized = email.ToUpperInvariant();
        var user = await db.AppUsers.SingleOrDefaultAsync(item => item.NormalizedEmail == normalized, cancellationToken);
        if (user is null)
        {
            user = new AppUser { Email = email, NormalizedEmail = normalized, DisplayName = "Super Admin", CreatedAtUtc = DateTime.UtcNow };
            db.AppUsers.Add(user);
        }
        user.Role = UserRoles.SuperAdmin;
        user.AdminStatus = AdminApprovalStatuses.Approved;
        user.IsActive = true;
        user.PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>().HashPassword(user, password);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogWarning("The first SuperAdmin was provisioned from secure configuration. Remove BootstrapSuperAdmin settings now.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
