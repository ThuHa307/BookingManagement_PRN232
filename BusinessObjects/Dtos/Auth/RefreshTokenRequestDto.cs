using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessObjects.Dtos.Auth
{
    public class RefreshTokenRequestDto
    {
        public int AccountId { get; set; }
        public string? RefreshToken { get; set; }
    }
}