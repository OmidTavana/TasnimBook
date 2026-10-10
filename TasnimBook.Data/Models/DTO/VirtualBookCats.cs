using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TasnimBook.Data
{
    public class VirtualBookCats
    {
        public VirtualBookCats()
        {

        }
        public int Id { get; set; }

        public string Title { get; set; }

        public string Url { get; set; }

        public string ImageUrl { get; set; }

        public string Description { get; set; }

        public DateTime? Date { get; set; }

        [StringLength(50)]
        public string FileSize { get; set; }

        [StringLength(50)]
        public string PagesCount { get; set; }

        public int? AuthorId { get; set; }

        public int? LikesCount { get; set; }

        public int? Score { get; set; }

        public Author Author { get; set; }
        public ICollection<BookCat> BookCategories { get; set; }
    }
}
