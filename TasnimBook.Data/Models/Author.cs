namespace TasnimBook.Data
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Author")]
    public partial class Author : ICloneable
    {

        public Author()
        {
        }
        [Required]
        [Column("id")]
        public int Id { get; set; }

        [StringLength(50)]
        [Column("name")]
        public string Name { get; set; }

        [StringLength(50)]
        [Column("family")]
        public string Family { get; set; }
        [NotMapped]
        public string FullName => Name +" "+ Family;
        public ICollection<Book> Books { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
