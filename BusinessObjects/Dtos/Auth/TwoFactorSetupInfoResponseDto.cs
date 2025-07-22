using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessObjects.Dtos.Auth
{
    public class TwoFactorSetupInfoResponseDto
    {
        public string SharedKey { get; set; } = null!;
        public string AuthenticatorUri { get; set; } = null!;
    }
}