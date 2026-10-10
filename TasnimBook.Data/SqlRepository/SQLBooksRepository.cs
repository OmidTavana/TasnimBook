using Microsoft.EntityFrameworkCore;
using TasnimBook.Data;
using TasnimBook.Data.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TasnimBook.Data.DataSource;
using TasnimBook.Data.Interfaces;
using System.Net.Http;
using System;

namespace TasnimBook.Data.Services
{
    public class SQLBooksRepository : IBooksRepository
    {
        private readonly DbContextAnti dbContext;
        public SQLBooksRepository(DbContextAnti _dbContext)
        {
            dbContext = _dbContext;
        }

        public async Task<HttpResponseMessage> Add(BookAndCategoriesDTO bookAndCategories)
        {
            HttpResponseMessage httpResponse = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
            Book book = bookAndCategories.Book;
            List<int> bookCats = bookAndCategories.BookCats;
            //int Id = (await dbContext.Books.MaxAsync(e => e.Id)) + 1;
            book.Id = 0;
            try
            {
                List<ManyToManyBookAndBookCatcs> manyToManyBooks = new List<ManyToManyBookAndBookCatcs>();
                foreach (var item in bookCats)
                {
                    ManyToManyBookAndBookCatcs x = new ManyToManyBookAndBookCatcs();
                    x.BookId = book.Id;
                    x.BookCategoryId = item;
                    manyToManyBooks.Add(x);
                }

                book.BookCategories = manyToManyBooks;
                dbContext.Books.Add(book);
                await dbContext.SaveChangesAsync();
                httpResponse.StatusCode = System.Net.HttpStatusCode.OK;
            }
            catch (System.Exception ex)
            {

                httpResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
            }

            return httpResponse;
        }

        public async Task<HttpResponseMessage> Edit(BookAndCategoriesDTO bookAndCategories)
        {
            List<ManyToManyBookAndBookCatcs> manyToManyBooks = new List<ManyToManyBookAndBookCatcs>();
            HttpResponseMessage httpResponse = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
            Book book = bookAndCategories.Book;
            List<int> bookCats = bookAndCategories.BookCats;
            foreach (var item in bookCats)
            {
                ManyToManyBookAndBookCatcs x = new ManyToManyBookAndBookCatcs();
                x.BookId = book.Id;
                x.BookCategoryId = item;
                manyToManyBooks.Add(x);
            }

            try
            {

                dbContext.Entry(book).State = EntityState.Modified;

                foreach (var item in await dbContext.BookCategoryRelations.Where(e => e.BookId == book.Id).ToListAsync())
                {
                    dbContext.BookCategoryRelations.Remove(item);
                }
                await dbContext.SaveChangesAsync();

                foreach (var item in manyToManyBooks)
                {
                    dbContext.Entry(item).State = EntityState.Added;
                }
                await dbContext.SaveChangesAsync();
                httpResponse.StatusCode = System.Net.HttpStatusCode.OK;

            }
            catch (System.Exception ex)
            {
                httpResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
            }
            return httpResponse;
        }

        public async Task<IEnumerable<BooksDTO>> Search(string keyword)
        {
            var data = (from book in dbContext.Books
                        where (book.Author.Name + " " + book.Author.Family).Contains(keyword) ||
                              book.Title.Contains(keyword)
                        select new BooksDTO {
                            AuthorId = book.AuthorId,
                            AuthorName = book.Author.Name + " " + book.Author.Family,
                            CategoryName = book.BookCategories.FirstOrDefault().BookCategories.CategoryName,
                            Date = book.Date,
                            Description = book.Description,
                            FileSize = book.FileSize,
                            Id = book.Id,
                            CategoryId = book.BookCategories.FirstOrDefault().BookCategories.Id,
                            ImageUrl = book.ImageUrl,
                            LikesCount = book.LikesCount,
                            PagesCount = book.PagesCount,
                            Score = book.Score,
                            Title = book.Title,
                            Url = book.Url,
                            IsSuggested=book.IsSuggested,
                            IsAudioBook=book.IsAudioBook
                        });
            return await Task.FromResult(data.ToList());

        }
        public async Task<IEnumerable<BooksDTO>> GetSuggestedBooks(int page, int size)
        {
            int y = page * size;
            int x = y - size;
            var data = (from book in dbContext.Books
                        .Where(e => e.IsSuggested.Value)
                        select new BooksDTO {
                            AuthorId = book.AuthorId,
                            AuthorName = book.Author.Name + " " + book.Author.Family,
                            CategoryName = book.BookCategories.FirstOrDefault().BookCategories.CategoryName,
                            Date = book.Date,
                            Description = book.Description,
                            FileSize = book.FileSize,
                            Id = book.Id,
                            CategoryId = book.BookCategories.FirstOrDefault().BookCategories.Id,
                            ImageUrl = book.ImageUrl,
                            LikesCount = book.LikesCount,
                            PagesCount = book.PagesCount,
                            Score = book.Score,
                            Title = book.Title,
                            Url = book.Url,
                                IsSuggested = book.IsSuggested,
                            IsAudioBook = book.IsAudioBook
                        }).Skip(x).Take(size).OrderByDescending(e => e.Date);
            return await Task.FromResult(data.ToList());

        }
        public async Task<IEnumerable<BooksDTO>> GetRandomBooks(int page, int size)
        {
            int y = page * size;
            int x = y - size;
            var data = (from book in dbContext.Books
                        select new BooksDTO {
                            AuthorId = book.AuthorId,
                            AuthorName = book.Author.Name + " " + book.Author.Family,
                            CategoryName = book.BookCategories.FirstOrDefault().BookCategories.CategoryName,
                            Date = book.Date,
                            Description = book.Description,
                            FileSize = book.FileSize,
                            Id = book.Id,
                            CategoryId = book.BookCategories.FirstOrDefault().BookCategories.Id,
                            ImageUrl = book.ImageUrl,
                            LikesCount = book.LikesCount,
                            PagesCount = book.PagesCount,
                            Score = book.Score,
                            Title = book.Title,
                            Url = book.Url   , IsSuggested = book.IsSuggested,
                            IsAudioBook = book.IsAudioBook
                        }).Skip(x).Take(size).OrderByDescending(e => e.Description);
            return await Task.FromResult(data.ToList());

        }
        public async Task<IEnumerable<BooksDTO>> GetLast10Books()
        {
            var data = (from book in dbContext.Books
                        select new BooksDTO {
                            AuthorId = book.AuthorId,
                            AuthorName = book.Author.Name + " " + book.Author.Family,
                            CategoryName = book.BookCategories.FirstOrDefault().BookCategories.CategoryName,
                            Date = book.Date,
                            Description = book.Description,
                            FileSize = book.FileSize,
                            Id = book.Id,
                            CategoryId = book.BookCategories.FirstOrDefault().BookCategories.Id,
                            ImageUrl = book.ImageUrl,
                            LikesCount = book.LikesCount,
                            PagesCount = book.PagesCount,
                            Score = book.Score,
                            Title = book.Title,
                            Url = book.Url,
                            IsSuggested = book.IsSuggested,
                            IsAudioBook = book.IsAudioBook
                        }).Take(10);
            return await Task.FromResult(data.ToList());

        }

        private bool BooksExists(int id)
        {
            return dbContext.Books.Include(e => e.Author).Any(e => e.Id == id);
        }


        public async Task<Book> GetBook(int id)
        {
            Book data = new Book();
            try
            {
                data = await dbContext.Books.Where(b => b.Id == id).Include(e => e.Author).Include(e => e.BookCategories).FirstOrDefaultAsync();
                return data;
            }
            catch (Exception ex)
            {
                return data;
            }
        }
        public async Task<IEnumerable<BooksDTO>> GetNewBooks(int page, int size)
        {
            int y = page * size;
            int x = y - size;
            var data = (from book in dbContext.Books
                        select new BooksDTO {
                            AuthorId = book.AuthorId,
                            AuthorName = book.Author.Name + " " + book.Author.Family,
                            CategoryName = book.BookCategories.FirstOrDefault().BookCategories.CategoryName,
                            Date = book.Date,
                            Description = book.Description,
                            FileSize = book.FileSize,
                            Id = book.Id,
                            CategoryId = book.BookCategories.FirstOrDefault().BookCategories.Id,
                            ImageUrl = book.ImageUrl,
                            LikesCount = book.LikesCount,
                            PagesCount = book.PagesCount,
                            Score = book.Score,
                            Title = book.Title,
                            Url = book.Url,
                            IsSuggested = book.IsSuggested,
                            IsAudioBook = book.IsAudioBook
                        }).OrderByDescending(e => e.Date).Skip(x).Take(size);
            return await Task.FromResult(data.ToList());
        }
        public async Task<IEnumerable<BooksDTO>> List()
        {
            var data = await dbContext.Books
                        .Include(e => e.BookCategories)
                        .ThenInclude(x=> x.BookCategories)
                        .Select(e => new BooksDTO {
                            AuthorId = e.AuthorId,
                            AuthorName = e.Author.Name + " " + e.Author.Family,
                            CategoryName = e.BookCategories.FirstOrDefault()==null?"": e.BookCategories.FirstOrDefault().BookCategories.CategoryName,
                            Date = e.Date,
                            Description = e.Description,
                            FileSize = e.FileSize,
                            Id = e.Id,
                            CategoryId = (e.BookCategories.FirstOrDefault()==null?0: e.BookCategories.FirstOrDefault().BookCategories.Id ) ,
                            ImageUrl = e.ImageUrl,
                            LikesCount = e.LikesCount,
                            PagesCount = e.PagesCount,
                            Score = e.Score,
                            Title = e.Title,
                            Url = e.Url,
                            IsSuggested = e.IsSuggested,
                            IsAudioBook = e.IsAudioBook
                        }).Take(10).ToListAsync();
            return data;
        }
        public async Task<IEnumerable<BooksDTO>> List(List<int> lst)
        {
            var data = (from book in dbContext.Books
                        .Where(e => lst.Contains(e.Id))
                        select new BooksDTO {
                            AuthorId = book.AuthorId,
                            AuthorName = book.Author.Name + " " + book.Author.Family,
                            CategoryName = book.BookCategories.FirstOrDefault().BookCategories.CategoryName,
                            Date = book.Date,
                            Description = book.Description,
                            FileSize = book.FileSize,
                            Id = book.Id,
                            CategoryId = book.BookCategories.FirstOrDefault().BookCategories.Id,
                            ImageUrl = book.ImageUrl,
                            LikesCount = book.LikesCount,
                            PagesCount = book.PagesCount,
                            Score = book.Score,
                            Title = book.Title,
                            Url = book.Url,
                            IsSuggested = book.IsSuggested,
                            IsAudioBook = book.IsAudioBook
                        });

            //if (data != null)
            //    booksDTOs.Add(data);
            return data;
        }
        public async Task<IEnumerable<BooksDTO>> ListByCatergory(int catID)
        {
            var data = await (from book in dbContext.Books
                         .Where(e => e.BookCategories.Any(ed => ed.BookCategoryId == catID))
                              select new BooksDTO {
                                  AuthorId = book.AuthorId,
                                  AuthorName = book.Author.Name + " " + book.Author.Family,
                                  CategoryName = book.BookCategories.FirstOrDefault().BookCategories.CategoryName,
                                  Date = book.Date,
                                  Description = book.Description,
                                  FileSize = book.FileSize,
                                  Id = book.Id,
                                  CategoryId = book.BookCategories.FirstOrDefault().BookCategories.Id,
                                  ImageUrl = book.ImageUrl,
                                  LikesCount = book.LikesCount,
                                  PagesCount = book.PagesCount,
                                  Score = book.Score,
                                  Title = book.Title,
                                  Url = book.Url,
                                  IsSuggested = book.IsSuggested,
                                  IsAudioBook = book.IsAudioBook
                              }).ToListAsync();

            return data;
        }

    }
}
