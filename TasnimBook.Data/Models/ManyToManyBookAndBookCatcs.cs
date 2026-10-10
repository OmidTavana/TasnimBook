using System.ComponentModel.DataAnnotations.Schema;

namespace TasnimBook.Data
{
    [Table("ManyToManyBookAndBookCatcs")]
    public class ManyToManyBookAndBookCatcs
    {
        public ManyToManyBookAndBookCatcs()
        {

        }
        [Column("bookId")]
        public int BookId { get; set; }
        public Book Books { get; set; }

        [Column("bookCatId")]

        public int BookCategoryId { get; set; }
        public BookCat BookCategories { get; set; }
    }
}
