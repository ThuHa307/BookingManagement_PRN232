using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IPasswordHasherCustom
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);

    }
}