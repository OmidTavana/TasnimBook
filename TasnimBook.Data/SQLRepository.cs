using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TasnimBook.Data.DataSource;
using TasnimBook.Data.Interfaces;

namespace TasnimBook.Data.Services
{
    public class SQLRepository : IRepository
    {
        private readonly DbContextOptions<DbContextAnti> _dbOptions;
        public SQLRepository(DbContextOptionsBuilder<DbContextAnti>
        dbOptionsBuilder)
        {

        }
        public IBooksRepository IBooks => new SQLBooksRepository(new DbContextAnti(_dbOptions));
        public IAuthorRepository IAuthors => new SQLAuthorsRepository(new DbContextAnti(_dbOptions));

        public ICategoryRepository ICategory => new SQLCatRepository(new DbContextAnti(_dbOptions));

        IAdminRepository IRepository.IAdmin => throw new NotImplementedException();
    }
}
