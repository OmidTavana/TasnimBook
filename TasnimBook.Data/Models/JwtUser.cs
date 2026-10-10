namespace TasnimBook.Data
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("jwtuser")]
    public partial class JwtUser
    {
        public JwtUser()
        {
        }
        [Column("id")]
        public int Id { get; set; }
        [Column("regdate")]
        public DateTime RegDate { get; set; }
        [Column("uniquecode")]
        public string UniqueCode { get; set; }

    }
}   
