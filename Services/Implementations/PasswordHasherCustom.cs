using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Services.Interfaces;

namespace Services.Implementations
{
    public class PasswordHasherCustom : IPasswordHasherCustom
    {
        public string HashPassword(string password)
        {
            // Work factor mặc định của BCrypt.Net-Next thường là 10, đủ tốt cho hầu hết các trường hợp.
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}