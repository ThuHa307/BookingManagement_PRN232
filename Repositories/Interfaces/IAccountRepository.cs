using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Domains;
using BusinessObjects.Dtos;
using BusinessObjects.Dtos.Account;
using BusinessObjects.Dtos.Common;

namespace Repositories.Interfaces
{
    public interface IAccountRepository : IGenericRepository<Account>
    {
        Task<Account> GetSystemAccountByEmailAsync(string email);
        Task<Account> CreateAsync(Account user);
        Task UpdateAsync(Account user);

        Task<Account> CreateExternalAccountAsync(ExternalAccountRegisterDto dto);

        Task<UserProfile?> GetProfileByAccountIdAsync(int accountId);
        Task UpdateProfileAsync(UserProfile profile);
        Task UpdateAvatarAsync(UserProfile profile, string avatarUrl);
        Task<Account?> GetAccountByEmailAsync(string email);
        Task Update(Account account);
        Task SetUserOnlineAsync(int userId, bool isOnline);
        Task UpdateLastActiveAsync(int userId);
        Task<Account> GetAccountById(int accountId);
        Task<bool> CheckEmailExistsAsync(string email);
        Task<bool> CheckUsernameExistsAsync(string username);
        Task<Account?> GetAccountByEmailOrUsernameAsync(string input);

        // CRUD methods for Admin
        Task<PagedResultDto<Account>> GetAccountsWithFilterAsync(AccountFilterDto filter);
        Task<Account?> GetAccountByIdWithProfileAsync(int accountId);
        Task<Account> CreateAccountAsync(AccountCreateDto dto);
        Task<bool> UpdateAccountAsync(int accountId, AccountUpdateDto dto);
        Task<bool> DeleteAccountAsync(int accountId);
        Task<bool> ToggleAccountActiveStatusAsync(int accountId);
        Task<bool> IsEmailExistsForOtherAccountAsync(string email, int excludeAccountId);
        Task<bool> IsUsernameExistsForOtherAccountAsync(string username, int excludeAccountId);
        Task<bool> HasActivePostsAsync(int accountId);
    }
}