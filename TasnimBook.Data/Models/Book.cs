namespace TasnimBook.Data
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Books")]
    public partial class Book
    {
        public Book()
        {
        }

        [Column("id")]

        public int Id { get; set; }

        [Column("title")]

        public string Title { get; set; }

        [Column("url")]

        public string Url { get; set; }

        [Column("imageurl")]

        public string ImageUrl { get; set; }

        [Column("des")]

        public string Description { get; set; }

        [Column("date")]

        public DateTime? Date { get; set; }

        [StringLength(50)]
        [Column("filesize")]
        public string FileSize { get; set; }

        [StringLength(50)]
        [Column("pagescount")]
        public string PagesCount { get; set; }

        [Column("idauthor")]

        public int? AuthorId { get; set; }

        [Column("likescount")]

        public int? LikesCount { get; set; }

        [Column("score")]

        public int? Score { get; set; }

        [Column("IsSuggested")]

        public bool? IsSuggested { get; set; }
        [Column("IsAudioBook")]
        public bool? IsAudioBook { get; set; }

        public Author Author { get; set; }

        public ICollection<Comments> Comments { get; set; }

        public ICollection<ManyToManyBookAndBookCatcs> BookCategories { get; set; }

    }
}   
