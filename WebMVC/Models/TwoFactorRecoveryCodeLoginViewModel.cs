using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebMVC.Models
{
    public class TwoFactorRecoveryCodeLoginViewModel
    {
        [Required(ErrorMessage = "Mã khôi phục là bắt buộc.")]
        public string RecoveryCode { get; set; } = null!;
    }
}