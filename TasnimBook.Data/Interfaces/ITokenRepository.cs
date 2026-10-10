using TasnimBook.Data.Models;
using TasnimBook.Data.Models.DTO;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TasnimBook.Data.Interfaces
{
    public interface ITokenRepository
    {
        Task<IEnumerable<JwtUser>> List();
        Task<JwtUser> FindUser(string uniqueID);
        Task<int> Add(JwtUser user, bool isNewItem);
        Task<AuthResult> GenerateToken(string unique,int uniqueId,byte[] key);
        Task<AuthResult> RefreshToken(TokenRequest tokenRequest, TokenValidationParameters tokenValidation, byte[] key);
    }
}
