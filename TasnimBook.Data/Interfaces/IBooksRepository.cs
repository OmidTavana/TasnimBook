using TasnimBook.Data;
using TasnimBook.Data.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;


namespace TasnimBook.Data.Interfaces
{
    public interface IBooksRepository
    {
        Task<IEnumerable<BooksDTO>> List();
        Task<IEnumerable<BooksDTO>> List(List<int> id);
        Task<HttpResponseMessage> Add(BookAndCategoriesDTO bookAndCategories);
        Task<HttpResponseMessage> Edit( BookAndCategoriesDTO book);
        Task<Book> GetBook(int id);
        Task<IEnumerable<BooksDTO>> GetLast10Books();
        Task<IEnumerable<BooksDTO>> GetSuggestedBooks(int start, int count);
        Task<IEnumerable<BooksDTO>> GetRandomBooks(int start, int count);
        Task<IEnumerable<BooksDTO>> Search(string keyword);
        Task<IEnumerable<BooksDTO>> GetNewBooks(int start, int count);
        Task<IEnumerable<BooksDTO>> ListByCatergory(int catID);

    }
}
