using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessObjects.Dtos.Auth
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Email không được để trống.")]
        public string? email { get; set; }
        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        [DataType(DataType.Password)]
        public string? passwordHasher { get; set; }
    }
}