namespace TasnimBook.Data
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    [Table("Bookcat")]
    public partial class BookCat : ICloneable
    {
        public BookCat()
        {
        }

        [Column("id")]

        public int Id { get; set; }

        [StringLength(50)]
        [Column("catname")]
        public string CategoryName { get; set; }

        public ICollection<ManyToManyBookAndBookCatcs> Books { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
