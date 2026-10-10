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
    class RestAuthorsRepository : IAuthorRepository
    {
        HttpClient _client;
        private readonly string baseUrl;
        public RestAuthorsRepository(string url, HttpClient client)
        {
            _client = client;// new HttpClient();
            baseUrl = url;
        }

        public async Task<IEnumerable<Author>> List()
        {
            var Authors = new List<Author>();

            var uri = new Uri(string.Format(baseUrl + "Authors/list", string.Empty));
            try
            {
                var response = await _client.GetAsync(uri);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Authors = JsonConvert.DeserializeObject<List<Author>>(content);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(@"\tERROR {0}", ex.Message);
            }

            return Authors;
        }

        public async Task<HttpResponseMessage> Add(Author author, bool isNew)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var json = JsonConvert.SerializeObject(author);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                response = await _client.PostAsync(string.Format(baseUrl + "authors/add/{0}", isNew), content);

            }
            catch (Exception ex)
            {

                Debug.WriteLine(@"\tERROR {0}", ex.Message);
            }

            return response;
        }
    }
}
