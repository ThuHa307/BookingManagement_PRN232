using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessObjects.Dtos.Auth
{
    public class TwoFactorEnableResponseDto
    {
        public List<string> RecoveryCodes { get; set; } = new List<string>();
    }
}