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
    public class CategoriesController : Controller
    {
        private readonly ICategoryRepository catRepo;
        public CategoriesController(ICategoryRepository _ICat)
        {
            catRepo = _ICat;
        }
        // GET: api/Books
        [ApiKeyAuth]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookCat>>> List()
        {
            return Ok(await catRepo.List());
        }

        [AdminApiKeyAuth]
        [HttpPost("{isNew}")]
        public async Task<ActionResult<BookCat>> Add([FromBody] BookCat Bookcat, bool isNew)
        {
            try
            {
                await catRepo.Add(Bookcat, isNew);
                return Ok();
            }
            catch (System.Exception ex)
            {

                return Forbid();
            }
        }


    }
}
