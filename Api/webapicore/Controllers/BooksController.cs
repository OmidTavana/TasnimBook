using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using TasnimBook.Data;
using TasnimBook.Data.Interfaces;
using TasnimBook.Data.Models;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using webapicore.Fillter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace webapicore.Controllers
{
    [Route("api/[controller]/[Action]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class BooksController : Controller
    {
        private readonly IBooksRepository bookRepo;

        public BooksController(IBooksRepository _IBooks)
        {
            bookRepo = _IBooks;
        }
        // GET: api/Books
        [HttpGet]
        [ApiKeyAuth]
        public async Task<ActionResult<IEnumerable<BooksDTO>>> Liist()
        {
            return Ok(await bookRepo.List());
        }
        [HttpGet]
        [ApiKeyAuth]
        public async Task<ActionResult<IEnumerable<BooksDTO>>> List([FromQuery(Name = "lst")] List<int> lst)
        {


            return Ok(await bookRepo.List(lst));
        }
        // GET: api/Books/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Book>> GetBook(int id)
        {
                return await bookRepo.GetBook(id);
            }
            [ApiKeyAuth]
            [HttpGet("{keyword}")]
            public async Task<ActionResult<IEnumerable<BooksDTO>>> Search(string keyword)
            {
                return Ok(await bookRepo.Search(keyword));
            }
            // GET: api/Books/5
            [ApiKeyAuth]
            [HttpGet("{start}/{count}")]
            public async Task<ActionResult<IEnumerable<BooksDTO>>> GetNewBooks(int start, int count)
            {
                return Ok(await bookRepo.GetNewBooks(start, count));
            }
            // GET: api/Books/5
            [ApiKeyAuth]
            [HttpGet("{start}/{count}")]
            public async Task<ActionResult<IEnumerable<BooksDTO>>> GetSuggestedBooks(int start, int count)
            {
                return Ok(await bookRepo.GetSuggestedBooks(start, count));
            }
            [ApiKeyAuth]
            [HttpGet("{start}/{count}")]
            public async Task<ActionResult<IEnumerable<BooksDTO>>> GetRandomBooks(int start, int count)
        {
            return Ok(await bookRepo.GetRandomBooks(start, count));
        }
        [HttpGet("{catID}")]
        public async Task<ActionResult<IEnumerable<BooksDTO>>> GetByCategory(int catID)
        {
            return Ok(await bookRepo.ListByCatergory(catID));
        }

        [ApiKeyAuth]
        public async Task<ActionResult<IEnumerable<BooksDTO>>> GetLast10Books()
        {
            try { return Ok(await bookRepo.GetLast10Books()); }
            catch (WebException ex)
            {
                using (HttpWebResponse respp = (HttpWebResponse)ex.Response)
                {
                    
                }
                return Ok();
            }
        }

        // POST: api/Books
        [AdminApiKeyAuth]
        [HttpPost]
        public async Task<ActionResult<Book>> Add([FromBody] BookAndCategoriesDTO bookAndCategories)
        {
            try
            {
                await bookRepo.Add(bookAndCategories);
                return Ok();
            }
            catch (System.Exception ex)
            {

                return Forbid();
            }
        }

        // PUT: api/Books/5
        [AdminApiKeyAuth]
        [HttpPost]

        public async Task<ActionResult> Edit([FromBody] BookAndCategoriesDTO book)
        {
            HttpResponseMessage result = await bookRepo.Edit(book);
            if (result.IsSuccessStatusCode)//if affected rows is more than 0
            {
                return Ok();
            }
            else
            {
                return Forbid();
            }
        }

        // DELETE: api/ApiWithActions/5
        [AdminApiKeyAuth]
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
