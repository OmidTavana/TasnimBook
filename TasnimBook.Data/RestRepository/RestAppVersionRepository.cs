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
    class RestAppVersionRepository : IAppVersionReository
    {
        HttpClient _client;
        private readonly string baseUrl;
        public RestAppVersionRepository(string url, HttpClient client)
        {
            _client = client;// new HttpClient();
            baseUrl = url;
        }

        public async Task<AppVersion> GetAppVersion(int platformId)
        {
            var AppVersion = new AppVersion();

            var uri = new Uri(string.Format(baseUrl + "AppVersion/get/{0}",platformId ));
            try
            {
                var response = await _client.GetAsync(uri);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    AppVersion = JsonConvert.DeserializeObject<AppVersion>(content);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(@"\tERROR {0}", ex.Message);
            }

            return AppVersion;
        }

        public async Task<HttpResponseMessage> Add(AppVersion AppVersion, bool isNew)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var json = JsonConvert.SerializeObject(AppVersion);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                response = await _client.PostAsync(string.Format(baseUrl + "AppVersion/add/{0}", isNew), content);

            }
            catch (Exception ex)
            {

                Debug.WriteLine(@"\tERROR {0}", ex.Message);
            }

            return response;
        }
    }
}
