using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TasnimBook.Data.Interfaces;
using TasnimBook.Data.Models.DTO;

namespace TasnimBook.Data.RestRepository
{
    public class RestAppVersionRepository : IAppVersionReository
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

            var platform = platformId == (int)Platforms.Windows ? "windows" : "android";
            var uri = new Uri(baseUrl + $"app-releases/latest?platform={platform}");
            try
            {
                var response = await _client.GetAsync(uri);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var release = JsonConvert.DeserializeObject<AppReleaseDto>(content);
                    if (release is not null)
                    {
                        AppVersion = new AppVersion
                        {
                            Id = release.Id,
                            PlatformId = release.PlatformId,
                            VersionName = release.Version,
                            VersionNumber = release.Build,
                            ReleaseNotes = release.ReleaseNotes,
                            Url = release.DownloadUrl,
                            ReleaseDate = release.PublishedAt,
                            FileSize = release.FileSize,
                            Sha256 = release.Sha256,
                            ContentType = release.ContentType,
                            IsMandatory = release.IsMandatory,
                            IsPublished = true,
                            IsActive = true
                        };
                    }
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
