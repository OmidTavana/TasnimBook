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
    public class SQLAuthorsRepository : IAuthorRepository
    {
        private readonly DbContextAnti dbContext;
        public SQLAuthorsRepository(DbContextAnti _dbContext)
        {
            dbContext = _dbContext;
        }

        public async Task<HttpResponseMessage> Add(Author author, bool isNew)
        {
            HttpResponseMessage httpResponse = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
            try
            {
                if (isNew)
                    dbContext.Authors.Add(author);
                else
                    dbContext.Authors.Update(author);

                await dbContext.SaveChangesAsync();
                httpResponse.StatusCode = System.Net.HttpStatusCode.OK;
            }
            catch (System.Exception ex)
            {

                httpResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
            }

            return httpResponse;
        }

        public async Task<IEnumerable<Author>> List()
        {
            var data = await dbContext.Authors.ToListAsync();
            return data;
        }

    }
}
