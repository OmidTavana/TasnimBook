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
    public class SQLCatRepository : ICategoryRepository
    {
        private readonly DbContextAnti dbContext;
        public SQLCatRepository(DbContextAnti _dbContext)
        {
            dbContext = _dbContext;
        }

        public async Task<HttpResponseMessage> Add(BookCat bookcat, bool isNew)
        {
            HttpResponseMessage httpResponse = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
            try
            {
                if (isNew)
                dbContext.BookCategories.Add(bookcat);
                else
                    dbContext.BookCategories.Update(bookcat);

                await dbContext.SaveChangesAsync();
                httpResponse.StatusCode = System.Net.HttpStatusCode.OK;
            }
            catch (System.Exception ex)
            {

                httpResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
            }

            return httpResponse;
        }

        public async Task<IEnumerable<BookCat>> List()
        {
            var data = await dbContext.BookCategories.ToListAsync();
            return data; 
        }

    }
}
