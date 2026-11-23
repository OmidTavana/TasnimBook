using TasnimBook.Data;
using Xunit;
using Microsoft.EntityFrameworkCore;
using TasnimBook.Data.DataSource;

namespace TasnimBook.Tests;

public sealed class AppVersionOrderingTests
{
    [Theory]
    [InlineData("1.1.0", 10, "1.0.9", 99, true)]
    [InlineData("1.0.0", 11, "1.0", 10, true)]
    [InlineData("1.0", 10, "1.0.0", 10, false)]
    [InlineData("1.0.0", 9, "1.0.1", 1, false)]
    public void IsNewer_compares_semantic_version_then_build(string latestVersion, int latestBuild, string currentVersion, int currentBuild, bool expected)
    {
        var latest = Release(Platforms.Android, latestVersion, latestBuild);
        Assert.Equal(expected, AppVersionOrdering.IsNewer(latest, currentVersion, currentBuild));
    }

    [Fact]
    public void Latest_ignores_drafts_and_inactive_releases()
    {
        var candidates = new[]
        {
            Release(Platforms.Android, "1.0.0", 1),
            Release(Platforms.Android, "3.0.0", 3, published: false),
            Release(Platforms.Android, "2.0.0", 2, active: false),
            Release(Platforms.Android, "1.1.0", 4)
        };
        Assert.Equal("1.1.0", AppVersionOrdering.Latest(candidates)!.VersionName);
    }

    [Fact]
    public void Latest_is_evaluated_per_platform()
    {
        var releases = new[] { Release(Platforms.Android, "2.0.0", 20), Release(Platforms.Windows, "5.0.0", 50) };
        Assert.Equal("2.0.0", AppVersionOrdering.Latest(releases.Where(item => item.PlatformId == (int)Platforms.Android))!.VersionName);
        Assert.Equal("5.0.0", AppVersionOrdering.Latest(releases.Where(item => item.PlatformId == (int)Platforms.Windows))!.VersionName);
    }

    [Fact]
    public void Centralized_release_migration_is_discoverable()
    {
        var options = new DbContextOptionsBuilder<DbContextAnti>().UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=TasnimBookMigrationCheck").Options;
        using var context = new DbContextAnti(options);
        Assert.Contains("20261124120000_CentralizedAppReleases", context.Database.GetMigrations());
    }

    private static AppVersion Release(Platforms platform, string version, int build, bool published = true, bool active = true) => new()
    {
        PlatformId = (int)platform, VersionName = version, VersionNumber = build, IsPublished = published, IsActive = active,
        Url = "/download", ReleaseNotes = "test", ReleaseDate = DateTime.UtcNow
    };
}
