using TasnimBook.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TasnimBook.Data.DataSource;
using TasnimBook.Data.Interfaces;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;

namespace TasnimBook.Data.Services
{
    public class SQLAppVersionRepository : IAppVersionReository
    {
        private readonly DbContextAnti dbContext;
        public SQLAppVersionRepository(DbContextAnti _dbContext)
        {
            dbContext = _dbContext;
        }

        public async Task<HttpResponseMessage> Add(AppVersion appVersion, bool isNew)
        {
            HttpResponseMessage httpResponse = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
            try
            {
                if (isNew)
                    dbContext.AppVersions.Add(appVersion);
                else
                    dbContext.AppVersions.Update(appVersion);

                await dbContext.SaveChangesAsync();
                httpResponse.StatusCode = System.Net.HttpStatusCode.OK;
            }
            catch (System.Exception ex)
            {

                httpResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
            }

            return httpResponse;
        }

        public async Task<AppVersion> GetAppVersion(int platformId)
        {
            var data = await dbContext.AppVersions.FirstOrDefaultAsync(e=>e.PlatformId==platformId);
            return data;
        }

        
    }
}
