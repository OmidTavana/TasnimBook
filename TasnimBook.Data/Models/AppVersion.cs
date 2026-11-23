using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TasnimBook.Data
{
    [Table("appversion")]
  public  class AppVersion
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("versionname")]
        [Required]
        public string VersionName { get; set; } = string.Empty;
        [Column("versionnumber")]
        public int VersionNumber { get; set; }
        [Column("releasenotes")]
        [Required]
        public string ReleaseNotes { get; set; } = string.Empty;
        [Column("url")]
        [Required]
        public string Url { get; set; } = string.Empty;
        [Column("releasedate")]
        public DateTime ReleaseDate { get; set; }
        [Column("platformid")]
        public int PlatformId { get; set; }
        [Column("ispublished")]
        public bool IsPublished { get; set; } = true;
        [Column("isactive")]
        public bool IsActive { get; set; } = true;
        [Column("ismandatory")]
        public bool IsMandatory { get; set; }
        [Column("filesize")]
        public long? FileSize { get; set; }
        [Column("sha256"), MaxLength(64)]
        public string? Sha256 { get; set; }
        [Column("storedfilename"), MaxLength(260)]
        public string? StoredFileName { get; set; }
        [Column("contenttype"), MaxLength(100)]
        public string? ContentType { get; set; }
        [NotMapped]
        public Platforms Platform { get; set; }

    }

    public static class AppVersionOrdering
    {
        public static AppVersion? Latest(IEnumerable<AppVersion> versions) => versions
            .Where(item => item.IsPublished && item.IsActive)
            .OrderByDescending(item => Parse(item.VersionName))
            .ThenByDescending(item => item.VersionNumber)
            .ThenByDescending(item => item.ReleaseDate)
            .FirstOrDefault();

        public static bool IsNewer(AppVersion latest, string? currentVersion, int currentBuild)
        {
            var versionComparison = Parse(latest.VersionName).CompareTo(Parse(currentVersion));
            return versionComparison > 0 || versionComparison == 0 && latest.VersionNumber > currentBuild;
        }

        private static Version Parse(string? value)
        {
            if (Version.TryParse(value?.Trim().TrimStart('v', 'V'), out var parsed)) return Normalize(parsed);
            var numeric = new string((value ?? string.Empty).TakeWhile(character => char.IsDigit(character) || character == '.').ToArray());
            return Version.TryParse(numeric, out parsed) ? Normalize(parsed) : new Version(0, 0, 0, 0);
        }

        private static Version Normalize(Version version) => new(
            Math.Max(0, version.Major),
            Math.Max(0, version.Minor),
            Math.Max(0, version.Build),
            Math.Max(0, version.Revision));
    }
    public enum Platforms
    {
        Android=1,
        Windows=2,
        Ios=3
    }
}
