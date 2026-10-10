using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using TasnimBook.Data.Interfaces;
using TasnimBook.Data.RestRepository;

namespace TasnimBook.Data.SqlRepository
{
    public class RestRepository :  IRepository
    {
        private readonly string _url;
        HttpClient _client;

        public RestRepository(string url, string? apiKey = null, string? accessToken = null)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("An API base address is required.", nameof(url));

            _url = url.TrimEnd('/') + "/";
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
            // Pass the handler to httpclient(from you are calling api)
            _client = new HttpClient(clientHandler);
            _client.Timeout = TimeSpan.FromSeconds(200);
            if (!string.IsNullOrWhiteSpace(apiKey))
                _client.DefaultRequestHeaders.Add("APIKey", apiKey);
            if (!string.IsNullOrWhiteSpace(accessToken))
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        }
        public IBooksRepository IBooks => new RestBooksRepository(_url, _client);
        public IAuthorRepository IAuthors => new RestAuthorsRepository(_url, _client);

        public ICategoryRepository ICategory => new RestCategoryRepository(_url, _client);
        public IAdminRepository IAdmin => new RestAdminRepository(_url, _client);
    }
}
