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
    public class AppVersionController : Controller
    {
        private readonly IAppVersionReository AppVersionRepo;
        public AppVersionController(IAppVersionReository _IAppVersion)
        {
            AppVersionRepo = _IAppVersion;
        }
        // GET: api/Books
        [ApiKeyAuth]
        [HttpGet("{platformid}")]
        public async Task<ActionResult<AppVersion>> Get(int platformid)
        {
            return Ok(await AppVersionRepo.GetAppVersion(platformid));
        }

        [AdminApiKeyAuth]
        [HttpPost("{isNew}")]
        public async Task<ActionResult<AppVersion>> Add([FromBody] AppVersion AppVersion,bool isNew)
        {
            try
            {
                await AppVersionRepo.Add(AppVersion, isNew);
                return Ok();
            }
            catch (System.Exception ex)
            {
                return Forbid();
            }
        }


    }
}
