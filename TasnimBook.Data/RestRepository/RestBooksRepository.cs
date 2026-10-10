using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TasnimBook.Data.Interfaces;
using TasnimBook.Data.Models;

namespace TasnimBook.Data
{
    public class RestBooksRepository : IBooksRepository
    {
        HttpClient _client;
        private readonly string baseUrl;

        public List<BooksDTO> Books { get; private set; } = new();

        public RestBooksRepository(string url, HttpClient client)
        {
            _client = client;// new HttpClient();
            baseUrl = url;
        }
        public async Task<HttpResponseMessage> Add(BookAndCategoriesDTO bookAndCategories)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var json = JsonConvert.SerializeObject(bookAndCategories);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                response = await _client.PostAsync(string.Format(baseUrl + "books/add"), content);

            }
            catch (Exception ex)
            {

                Debug.WriteLine(@"\tERROR {0}", ex.Message);
            }

            return response;
        }

        public async Task<HttpResponseMessage> Edit(BookAndCategoriesDTO bookAndCategories)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var json = JsonConvert.SerializeObject(bookAndCategories);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                response = await _client.PostAsync(string.Format(baseUrl + "books/edit"), content);

            }
            catch (Exception ex)
            {

                Debug.WriteLine(@"\tERROR {0}", ex.Message);
            }

            return response;
        }

        public async Task<Book> GetBook(int id)
        {
            var book = new Book();

            var uri = new Uri(string.Format(baseUrl + "books/GetBook/{0}", id));
            try
            {
                var response = await _client.GetAsync(uri);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    book = JsonConvert.DeserializeObject<Book>(content);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(@"\tERROR {0}", ex.Message);
            }

            return book;
        }

        public async Task<IEnumerable<BooksDTO>> GetLast10Books()
        {
            return await GetMany("books/GetLast10Books");
        }
        public async Task<IEnumerable<BooksDTO>> GetNewBooks(int start, int count)
        {
            Books = new List<BooksDTO>();

            return await GetMany($"books/GetNewBooks/{start}/{count}");
        }
        public async Task<IEnumerable<BooksDTO>> GetSuggestedBooks(int start, int count)
        {
            Books = new List<BooksDTO>();

            return await GetMany($"books/GetSuggestedBooks/{start}/{count}");
        }
        public async Task<IEnumerable<BooksDTO>> GetRandomBooks(int start, int count)
        {
            Books = new List<BooksDTO>();

            return await GetMany($"books/GetRandomBooks/{start}/{count}");
        }

        public async Task<IEnumerable<BooksDTO>> List() => await GetMany("books/Liist");

        public async Task<IEnumerable<BooksDTO>> List(List<int> id)
        {
            var query = string.Join("&", id.Select(value => "lst=" + value));
            return await GetMany("books/List?" + query);
        }

        public async Task<IEnumerable<BooksDTO>> ListByCatergory(int catID) => await GetMany($"books/GetByCategory/{catID}");

        public async Task<IEnumerable<BooksDTO>> Search(string keyword)
        {
            Books = new List<BooksDTO>();

            var uri = new Uri(baseUrl + "books/Search/" + Uri.EscapeDataString(keyword));
            try
            {
                var response = await _client.GetAsync(uri);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Books = JsonConvert.DeserializeObject<List<BooksDTO>>(content);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(@"\tERROR {0}", ex.Message);
            }

            return Books;
        }

        private async Task<IEnumerable<BooksDTO>> GetMany(string relativePath)
        {
            Books = new List<BooksDTO>();
            try
            {
                var response = await _client.GetAsync(new Uri(baseUrl + relativePath));
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Books = JsonConvert.DeserializeObject<List<BooksDTO>>(content) ?? new List<BooksDTO>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(@"\tERROR {0}", ex.Message);
            }

            return Books;
        }
    }
}
