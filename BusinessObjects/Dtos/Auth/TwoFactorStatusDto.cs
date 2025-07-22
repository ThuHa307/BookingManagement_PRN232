using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessObjects.Dtos.Auth
{
    public class TwoFactorStatusDto
    {
        public bool IsTwoFactorEnabled { get; set; }
    }
}