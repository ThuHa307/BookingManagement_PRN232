using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BusinessObjects.Domains;

namespace Services.Interfaces
{
    public interface IAccountService
    {
        Task<Account> GetAccountByEmailAsync(string email);
        Task<IEnumerable<Account>> GetAllAccountsAsync();
        Task<bool> UpdateAccountAsync(int id, Account updatedAccount);
        Task<bool> DeleteAccountAsync(int id);
    }
}