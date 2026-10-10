using TasnimBook.Data;
using TasnimBook.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace TasnimBook.Data.Interfaces
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<BookCat>> List();
        Task<HttpResponseMessage> Add(BookCat bookcat,bool isNew);
    }
}
