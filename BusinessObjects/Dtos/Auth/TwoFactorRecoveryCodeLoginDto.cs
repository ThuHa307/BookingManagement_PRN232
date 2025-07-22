using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessObjects.Dtos.Auth
{
    public class TwoFactorRecoveryCodeLoginDto
    {
        [Required(ErrorMessage = "Recovery code is required.")]
        public string RecoveryCode { get; set; } = null!;
    }
}