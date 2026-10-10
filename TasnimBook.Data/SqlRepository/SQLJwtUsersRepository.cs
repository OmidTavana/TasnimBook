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
    public class SQLJwtUsersRepository : IJwtUserRepository
    {
        private readonly DbContextAnti dbContext;
        public SQLJwtUsersRepository(DbContextAnti _dbContext)
        {
            dbContext = _dbContext;
        }

        public async Task<HttpResponseMessage> Add(JwtUser JwtUser, bool isNew)
        {
            HttpResponseMessage httpResponse = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
            try
            {
                if (isNew)
                    dbContext.JwtUser.Add(JwtUser);
                else
                    dbContext.JwtUser.Update(JwtUser);

                await dbContext.SaveChangesAsync();
                httpResponse.StatusCode = System.Net.HttpStatusCode.OK;
            }
            catch (System.Exception ex)
            {

                httpResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
            }

            return httpResponse;
        }

        public async Task<IEnumerable<JwtUser>> List()
        {
            var data = await dbContext.JwtUser.ToListAsync();
            return data;
        }

    }
}
