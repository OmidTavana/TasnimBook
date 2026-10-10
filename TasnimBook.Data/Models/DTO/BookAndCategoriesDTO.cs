using System;
using System.Collections.Generic;
using System.Text;

namespace TasnimBook.Data.Models{
    public class BookAndCategoriesDTO
    {
        public Book Book { get; set; }
        public List<int> BookCats { get; set; }
    }
}
