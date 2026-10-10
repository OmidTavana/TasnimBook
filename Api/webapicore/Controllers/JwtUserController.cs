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
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using TasnimBook.Data.Interfaces;
using TasnimBook.Data.Services;
using Microsoft.Extensions.Configuration;
using TasnimBook.Data.Models.DTO;

namespace webapicore.Controllers
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    [ApiKeyAuth]
    //[AdminApiKeyAuth]
    public class JwtUserController : Controller
    {
        JwtConfig jwtConfig;
        IConfigurationRoot configuration;
        TokenValidationParameters validationParameters;
        ITokenRepository jwtUserRepository;

        public JwtUserController(ITokenRepository _jwtUserRepository, IConfigurationRoot _configuration, TokenValidationParameters _validationParameters)
        {
            jwtUserRepository = _jwtUserRepository;
            configuration = _configuration;
            validationParameters = _validationParameters;
        }
        [HttpPost("{uniqueCode}")]
        public async Task<ObjectResult> Register([FromRoute] string uniqueCode)
        {
            try
            {
                //Find User by Unique code
                var user = await jwtUserRepository.FindUser(uniqueCode);
                int userId = 0;

                //if user is Already Registerd 
                if (user == null)
                    //add new user
                    userId = await jwtUserRepository.Add(new JwtUser { RegDate = DateTime.Now, UniqueCode = uniqueCode }, true);
                else
                    userId = user.Id;
                //if user is stored
                if (userId != -200)
                {
                    //find key
                    var key = Encoding.ASCII.GetBytes(configuration["jwtConfig:Secret"]);
                    //generate token
                    var jwtToken = await jwtUserRepository.GenerateToken(uniqueCode, userId, key);
                            
                    return Ok(jwtToken);

                }
                else
                    return BadRequest("user Not added");

            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
        [HttpPost]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequest tokenRequest)
        {
            if (ModelState.IsValid)
            {
                var key = Encoding.ASCII.GetBytes(configuration["jwtConfig:Secret"]);

                var result = await jwtUserRepository.RefreshToken(tokenRequest, validationParameters, key);
                if (result == null)
                {
                    return BadRequest(new RegisterationTokenResponse()
                    {
                        Errors = new List<string>()
                        {
                         "Invalid Payload"
                        },
                        Success = false
                    });
                }
                return Ok(result);

            }
            return BadRequest(new RegisterationTokenResponse()
            {
                Errors = new List<string>()
                {
                    "Invalid Payload"
                },
                Success = false
            });
        }
    }
}
