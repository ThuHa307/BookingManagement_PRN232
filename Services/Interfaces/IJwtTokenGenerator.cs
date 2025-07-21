using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Domains;

namespace Services.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(Account user);
    }
}