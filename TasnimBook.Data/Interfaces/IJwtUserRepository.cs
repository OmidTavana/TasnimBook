using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TasnimBook.Data.Interfaces
{
    public interface IJwtUserRepository
    {
        Task<IEnumerable<JwtUser>> List();
        Task<HttpResponseMessage> Add(JwtUser user, bool isNewItem);
    }
}
