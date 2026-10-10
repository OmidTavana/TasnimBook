using System;
using System.Collections.Generic;
using System.Text;

namespace TasnimBook.Data.Interfaces
{
    public interface IRepository
    {
        IBooksRepository IBooks { get; }
        IAuthorRepository IAuthors { get; }
        ICategoryRepository ICategory { get; }
        IAdminRepository IAdmin { get; }
    }
}
