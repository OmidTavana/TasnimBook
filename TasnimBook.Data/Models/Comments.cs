namespace TasnimBook.Data
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    [Table("Comments")]
    public partial class Comments
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("authoranswer")]

        public string AuthorAnswer { get; set; }

        [Column("comment")]

        public string Comment { get; set; }

        [Column("showinsite")]

        public bool? ShowInSite { get; set; }

        [Column("idbook")]

        public int? BookId { get; set; }

        [StringLength(50)]
        [Column("iduser")]
        public string UserId { get; set; }

        public Book Books { get; set; }

    }
}
