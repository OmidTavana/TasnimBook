using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TasnimBook.Data.Interfaces
{
    public interface IAppVersionReository
    {
        Task<AppVersion> GetAppVersion(int platformId);
        Task<HttpResponseMessage> Add(AppVersion appVersion, bool isNewItem);
    }
}
