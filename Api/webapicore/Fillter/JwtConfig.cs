using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace webapicore.Fillter
{
    public class JwtConfig
    {
        public string Secret { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int AccessTokenMinutes { get; set; } = 30;
        public int RefreshTokenDays { get; set; } = 30;
    }
}
