using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TasnimBook.Data.Interfaces;

namespace TasnimBook.Data.RestRepository
{
    class RestCategoryRepository : ICategoryRepository
    {
        HttpClient _client;
        private readonly string baseUrl;
        public RestCategoryRepository(string url, HttpClient client)
        {
            _client = client;// new HttpClient();
            baseUrl = url;
        }

        public async Task<IEnumerable<BookCat>> List()
        {
            var Bookcats = new List<BookCat>();

            var uri = new Uri(string.Format(baseUrl + "Categories/list", string.Empty));
            try
            {
                var response = await _client.GetAsync(uri);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Bookcats = JsonConvert.DeserializeObject<List<BookCat>>(content);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(@"\tERROR {0}", ex.Message);
            }

            return Bookcats;
        }
        public async Task<HttpResponseMessage> Add(BookCat Bookcat, bool isNew)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var json = JsonConvert.SerializeObject(Bookcat);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                response = await _client.PostAsync(string.Format(baseUrl + "categories/add/{0}", isNew), content);

            }
            catch (Exception ex)
            {

                Debug.WriteLine(@"\tERROR {0}", ex.Message);
            }

            return response;
        }
    }
}
