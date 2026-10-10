namespace TasnimBook.Data
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Bookcase")]
    public partial class BookCase
    {
        [Column("id")]
        public int Id { get; set; }

        [StringLength(50)]
        [Column("name")]
        public string Name { get; set; }

        [StringLength(50)]
        [Column("iduser")]
        public string UserId { get; set; }

    }
}
