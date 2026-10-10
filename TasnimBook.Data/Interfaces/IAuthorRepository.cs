using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TasnimBook.Data.Interfaces
{
    public interface IAuthorRepository
    {
        Task<IEnumerable<Author>> List();
        Task<HttpResponseMessage> Add(Author author, bool isNewItem);
    }
}
