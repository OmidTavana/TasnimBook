using Microsoft.AspNetCore.Mvc;
using TasnimBook.Data;
using TasnimBook.Data.Interfaces;
using TasnimBook.Data.Models;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using webapicore.Fillter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace webapicore.Controllers
{
    [Route("api/[controller]/[Action]")]
    //[ApiKeyAuth]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AuthorsController : Controller
    {
        private readonly IAuthorRepository authorRepo;
        public AuthorsController(IAuthorRepository _IAuthor)
        {
            authorRepo = _IAuthor;
        }
        // GET: api/Books
        [ApiKeyAuth]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Author>>> List()
        {
            return Ok(await authorRepo.List());
        }

        [AdminApiKeyAuth]
        [HttpPost("{isNew}")]
        public async Task<ActionResult<Author>> Add([FromBody] Author author,bool isNew)
        {
            try
            {
                await authorRepo.Add(author, isNew);
                return Ok();
            }
            catch (System.Exception ex)
            {

                return Forbid();
            }
        }


    }
}
