using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Domains;
using DataAccessObjects.DB;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class AccountRepository : GenericRepository<Account>, IAccountRepository
    {
        public AccountRepository(RentNestSystemContext context) : base(context)
        {
        }

        public async Task<Account> GetSystemAccountByEmailAsync(string email)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email.Equals(email));
            if (account is null)
            {
                return null!;
            }
            return account;
        }
        public async Task<Account> CreateAsync(Account user)
        {
            _context.Accounts.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task UpdateAsync(Account user)
        {
            _context.Accounts.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}