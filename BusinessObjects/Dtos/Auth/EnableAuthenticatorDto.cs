using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessObjects.Dtos.Auth
{
    public class EnableAuthenticatorDto
    {
        public string? SharedKey { get; set; }
        public string? AuthenticatorUri { get; set; }
    }
}