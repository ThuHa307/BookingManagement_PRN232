using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessObjects.Dtos.Auth
{
    public class UserInfoResponseDto
    {
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
    }
}