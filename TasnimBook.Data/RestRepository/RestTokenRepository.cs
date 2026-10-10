using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TasnimBook.Data.Interfaces;
using TasnimBook.Data.Models;
using Microsoft.Extensions.Configuration;
using TasnimBook.Data.Models.DTO;
using Microsoft.IdentityModel.Tokens;

namespace TasnimBook.Data.RestRepository
{
    class RestTokenRepository : ITokenRepository
    {
        HttpClient _client;
        private readonly string baseUrl;
        public RestTokenRepository(string url, HttpClient client)
        {
            _client = client;// new HttpClient();
            baseUrl = url;
        }

        public async Task<IEnumerable<JwtUser>> List()
        {
            var JwtUsers = new List<JwtUser>();

            var uri = new Uri(string.Format(baseUrl + "JwtUsers/list", string.Empty));
            try
            {
                var response = await _client.GetAsync(uri);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    JwtUsers = JsonConvert.DeserializeObject<List<JwtUser>>(content);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(@"\tERROR {0}", ex.Message);
            }

            return JwtUsers;
        }

        public async Task<int> Add(JwtUser JwtUser, bool isNew)
        {
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var json = JsonConvert.SerializeObject(JwtUser);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                response = await _client.PostAsync(string.Format(baseUrl + "JwtUsers/add/{0}", isNew), content);

            }
            catch (Exception ex)
            {

                Debug.WriteLine(@"\tERROR {0}", ex.Message);
            }

            return -200;
        }

        public Task<AuthResult> GenerateToken(string unique, int uniqueId, byte[] key)
        {
            throw new NotImplementedException();
        }

        public Task<AuthResult> RefreshToken(TokenRequest tokenRequest, TokenValidationParameters tokenValidation, byte[] key)
        {
            throw new NotImplementedException();
        }

        public Task<JwtUser> FindUser(string id)
        {
            throw new NotImplementedException();
        }
    }
}
