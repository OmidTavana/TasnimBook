using TasnimBook.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TasnimBook.Data.DataSource;
using TasnimBook.Data.Interfaces;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using TasnimBook.Data.Models;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TasnimBook.Data.Models.DTO;

namespace TasnimBook.Data.Services
{
    public class SQLTokenRepository : ITokenRepository
    {
        private readonly DbContextAnti dbContext;
        public SQLTokenRepository(DbContextAnti _dbContext)
        {
            dbContext = _dbContext;
        }

        public async Task<int> Add(JwtUser JwtUser, bool isNew)
        {
            int newId = -200;
            HttpResponseMessage httpResponse = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
            try
            {
                if (isNew)
                    dbContext.JwtUser.Add(JwtUser);
                else
                    dbContext.JwtUser.Update(JwtUser);

                await dbContext.SaveChangesAsync();
                newId = JwtUser.Id;
                httpResponse.StatusCode = System.Net.HttpStatusCode.OK;
            }
            catch (System.Exception ex)
            {

                httpResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
            }

            return newId;
        }

        public async Task<AuthResult> GenerateToken(string unique, int uniqueId, byte[] key)
        {
            var exTime = 300000;
            var tokenExpireTimeStamp = new DateTimeOffset(DateTime.UtcNow.AddMilliseconds(exTime));
            var jwtTokenHander = new JwtSecurityTokenHandler();
            //generate Claims
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("unique", unique),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Nbf,new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString()),
                    new Claim(JwtRegisteredClaimNames.Exp,tokenExpireTimeStamp.ToUnixTimeSeconds().ToString()),

                }),
                Expires = DateTime.UtcNow.AddMilliseconds(exTime),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            //create token
            var securityToken = jwtTokenHander.CreateToken(tokenDescriptor);
            //generate jwt token out of it
            var jwtToken = jwtTokenHander.WriteToken(securityToken);
            //generate refresh token
            RefreshToken newrefreshToken = new RefreshToken()
            {
                IsUsed = false,
                IsRevorked = false,
                JwtId = securityToken.Id,
                UserId = uniqueId,
                AddedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddMonths(6),
                Token = RandomString(35) + Guid.NewGuid()
            };
            //save refresh token
            await dbContext.RefreshTokens.AddAsync(newrefreshToken);
            await dbContext.SaveChangesAsync();
            //return result
            return new AuthResult() { Token = jwtToken, RefreshToken = newrefreshToken.Token, Status = AuthResultMessages.Success.ToString(), StatusCode = (int)AuthResultMessages.Success, ExpiresAt = exTime.ToString() };
        }

        private async Task<AuthResult> VerifyAndGenerateToken(TokenRequest tokenRequest, TokenValidationParameters validationParameters, byte[] key)
        {
            var jwtValidationParameters2 = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ClockSkew = TimeSpan.Zero,
                RequireExpirationTime = true
            };
            //Validation 1 -- Validate JWT Token Format
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            try
            {
                ClaimsPrincipal tokenInVerification = new ClaimsPrincipal();
                tokenInVerification = jwtTokenHandler.ValidateToken(tokenRequest.Token, jwtValidationParameters2, out var validationParams);
                //Validation 2 -- Validate encription alg
                if (validationParams is JwtSecurityToken jwtSecurityToken)
                {
                    var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);
                    if (result == false)
                        return null;
                }

                //validation 3 -- validate expire date
                var utcExpiryDate = long.Parse(tokenInVerification.Claims.FirstOrDefault(e => e.Type == JwtRegisteredClaimNames.Exp).Value);
                var expiraryDate = UnixTineStampToDatetime(utcExpiryDate);
                if (expiraryDate > DateTime.UtcNow)
                {
                    return new AuthResult()
                    {
                        Status = AuthResultMessages.TokenHasNotYetExpired.ToString(),
                        StatusCode = (int)AuthResultMessages.TokenHasNotYetExpired
                    };
                }



                //validation 4 -- validate existance the token
                var storedToken = await dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Token == tokenRequest.RefreshToken);
                if (storedToken == null)
                {
                    return new AuthResult()
                    {
                        Status = AuthResultMessages.TokenDoesNotExist.ToString(),
                        StatusCode = (int)AuthResultMessages.TokenDoesNotExist
                    };
                }
                //validation 5 -- validate if used
                if (storedToken.IsUsed)
                {
                    return new AuthResult()
                    {
                        Status = AuthResultMessages.TokenHasBeenUsed.ToString(),
                        StatusCode = (int)AuthResultMessages.TokenHasBeenUsed
                    };
                }
                //validation 6 -- validate if revorked
                if (storedToken.IsRevorked)
                {
                    return new AuthResult()
                    {
                        Status = AuthResultMessages.TokenHasBeenRevorked.ToString(),
                        StatusCode = (int)AuthResultMessages.TokenHasBeenRevorked
                    };
                }

                //Validation 7 -- validate the id
                var jti = tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;
                if (storedToken.JwtId != jti)
                {
                    return new AuthResult()
                    {
                        Status = AuthResultMessages.TokenDoesntMatch.ToString(),
                        StatusCode = (int)AuthResultMessages.TokenDoesntMatch
                    };
                }
                //update current token 
                storedToken.IsUsed = true;
                dbContext.RefreshTokens.Update(storedToken);
                await dbContext.SaveChangesAsync();
                //Generate a new token
                var dbUser = await dbContext.JwtUser.Where(e => e.Id == storedToken.UserId).FirstOrDefaultAsync();
                return await GenerateToken(dbUser.UniqueCode, dbUser.Id, key);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private DateTime UnixTineStampToDatetime(long utcExpiryDate)
        {
            var dateTimeVal = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTimeVal = dateTimeVal.AddMilliseconds(utcExpiryDate).ToUniversalTime();
            return dateTimeVal;
        }

        private string RandomString(int lenght)
        {
            var random = new Random();
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789!@#$%^&*()_+";
            return new string(Enumerable.Repeat(chars, lenght)
                .Select(x => x[random.Next(x.Length)]).ToArray());
        }

        public async Task<IEnumerable<JwtUser>> List()
        {
            var data = await dbContext.JwtUser.ToListAsync();
            return data;
        }


        public async Task<AuthResult> RefreshToken(TokenRequest tokenRequest, TokenValidationParameters tokenValidation, byte[] key)
        {
            return await VerifyAndGenerateToken(tokenRequest, tokenValidation, key);
        }

        public async Task<JwtUser> FindUser(string uniqueCode)
        {
            var data = await dbContext.JwtUser.Where(s => s.UniqueCode.Equals(uniqueCode)).FirstOrDefaultAsync();
            return data;
        }
    }
}
