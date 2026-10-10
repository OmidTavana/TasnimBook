using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch.Internal;
using Microsoft.AspNetCore.Mvc;
using TasnimBook.Data;
using TasnimBook.Data.Models;
using webapicore.Fillter;
using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace webapicore.Controllers
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    //[ApiKeyAuth]
    [AdminApiKeyAuth]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AdminController : Controller
    {
        private static IWebHostEnvironment _env;
        public AdminController(IWebHostEnvironment _hostingEnvironment)
        {
            _env = _hostingEnvironment;
        }
        [HttpPost]
        public Task<HttpResponseMessage> Register(string uqniqeCode)
        {
            return null;
        }

        [HttpPost]
        public string UploadFile()
        {
            try
            {
                var httpquest = HttpContext.Request;
                if (httpquest.Form.Files.Count > 0)
                {
                    foreach (var file in httpquest.Form.Files)
                    {
                        if (!Directory.Exists(_env.WebRootPath + "/Upload/"))
                        {
                            Directory.CreateDirectory(_env.WebRootPath + "/Upload/");

                        }
                        using (FileStream fileStream = System.IO.File.Create(_env.WebRootPath + "/Upload/" + file.FileName, 1048576))
                        {
                            file.CopyTo(fileStream);
                            fileStream.Flush();
                            return "/Upload/" + file.FileName;
                        }
                    }
                        return "No File Found!";
                }
                else
                {
                    return "Failed";
                }
            }
            catch (Exception ex)
            {

                return ex.Message;
            }

        }
    }
}
