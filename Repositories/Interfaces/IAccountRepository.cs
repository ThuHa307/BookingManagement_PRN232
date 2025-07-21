using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Domains;

namespace Repositories.Interfaces
{
    public interface IAccountRepository : IGenericRepository<Account>
    {
        Task<Account> GetSystemAccountByEmailAsync(string email);
        Task<Account> CreateAsync(Account user);
        Task UpdateAsync(Account user);
    }
}