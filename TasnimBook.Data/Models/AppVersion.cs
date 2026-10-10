using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace TasnimBook.Data
{
    [Table("appversion")]
  public  class AppVersion
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("versionname")]
        public string VersionName { get; set; }
        [Column("versionnumber")]
        public int VersionNumber { get; set; }
        [Column("releasenotes")]
        public string ReleaseNotes { get; set; }
        [Column("url")]
        public string Url { get; set; }
        [Column("releasedate")]
        public DateTime ReleaseDate { get; set; }
        [Column("platformid")]
        public int PlatformId { get; set; }
        [NotMapped]
        public Platforms Platform { get; set; }

    }
    public enum Platforms
    {
        Android=1,
        Windows=2,
        Ios=3
    }
}
