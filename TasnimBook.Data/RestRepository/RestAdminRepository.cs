using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TasnimBook.Data.Interfaces;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TasnimBook.Data.RestRepository
{
    class RestAdminRepository : IAdminRepository
    {
        HttpClient _client;
        private readonly string baseUrl;
        public RestAdminRepository(string url, HttpClient client)
        {
            _client = client;// new HttpClient();
            baseUrl = url;
        }

        public async Task<string> UploadFile(byte[] file, string name)
        {
            string response = "";
            try
            {
                _client.Timeout = TimeSpan.FromSeconds(200);
                using (var content =
                    new MultipartFormDataContent("Upload----" + DateTime.Now.ToString(CultureInfo.InvariantCulture)))
                {
                    content.Add(new StreamContent(new MemoryStream(file)));

                    using (
                       var message =
                           await _client.PostAsync(string.Format(baseUrl + "Admin/UploadFile"), content))
                    {
                        var input = await message.Content.ReadAsStringAsync();

                        return !string.IsNullOrWhiteSpace(input) ? Regex.Match(input, @"http://\w*\.directupload\.net/images/\d*/\w*\.[a-z]{3}").Value : null;
                    }
                }

            }
            catch (Exception ex)
            {

                Debug.WriteLine(@"\tERROR {0}", ex.Message);
            }

            return response;
        }
    }
}
