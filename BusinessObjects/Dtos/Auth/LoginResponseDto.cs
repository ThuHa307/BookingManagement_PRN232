using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessObjects.Dtos.Auth
{
    public class LoginResponseDto
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public bool RequiresTwoFactor { get; set; }
        // Có thể thêm thông tin role nếu muốn client biết vai trò ngay sau login
        public string? Role { get; set; }
    }
}