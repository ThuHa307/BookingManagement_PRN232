using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebMVC.Models
{
    public class TwoFactorVerificationViewModel
    {
        [Required(ErrorMessage = "Mã xác thực là bắt buộc.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã xác thực phải có 6 chữ số.")]
        public string Code { get; set; } = string.Empty; // Đảm bảo có trường Code
        public string? Email { get; set; } // Có thể có để hiển thị lại
    }
}