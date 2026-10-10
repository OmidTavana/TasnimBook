using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace TasnimBook.Data
{
    [Table("refreshtoken")]
  public  class RefreshToken
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("userid")]
        public int UserId { get; set; }
        [Column("token")]
        public string Token { get; set; }
        [Column("isused")]
        public bool IsUsed { get; set; }
        [Column("jwtid")]
        public string JwtId { get; set; }
        [Column("isrevorked")]
        public bool IsRevorked { get; set; }
        [Column("addeddate")]
        public DateTime AddedDate { get; set; }
        [Column("expirydate")]
        public DateTime ExpiryDate { get; set; }
        [ForeignKey(nameof(UserId))]
        public virtual  JwtUser User { get; set; }
    }
}
