using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TasnimBook.Data.Models
{
    public class AuthResult
    {
        public int StatusCode { get; set; }
        public string Status { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public string ExpiresAt { get; set; }
    }
    public enum AuthResultMessages
    {
        Success = 1,
        TokenHasNotYetExpired = -100,
        TokenDoesNotExist = -200,
        TokenHasBeenRevorked = -300,
        TokenDoesntMatch = -400,
        TokenHasBeenUsed = -500
    }
}
