using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessObjects.Dtos.Auth
{
    public class RefreshTokenRequestDto
    {
        [Required]
        public string? RefreshToken { get; set; }
        [Required]
        public string? Email { get; set; }
    }
}