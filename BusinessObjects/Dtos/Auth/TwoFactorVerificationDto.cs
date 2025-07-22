using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessObjects.Dtos.Auth
{
    public class TwoFactorVerificationDto
    {
        [Required]
        [StringLength(7, ErrorMessage = "Mã {0} phải dài ít nhất {2} và tối đa {1} ký tự.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        public string? Code { get; set; }
    }
}