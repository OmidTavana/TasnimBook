using System;

namespace TasnimBook.Data.Models
{
    public class BooksDTO
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Url { get; set; }

        public string ImageUrl { get; set; }

        public string Description { get; set; }

        public DateTime? Date { get; set; }

        public string FileSize { get; set; }

        public string PagesCount { get; set; }

        public int? AuthorId { get; set; }

        public int? LikesCount { get; set; }

        public int? Score { get; set; }
        public string AuthorName { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public bool? IsAudioBook { get; set; }
        public bool? IsSuggested { get; set; }

    }
}
