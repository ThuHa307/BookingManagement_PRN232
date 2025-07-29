using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BusinessObjects.Domains;
using BusinessObjects.Dtos;
using BusinessObjects.Dtos.Account;
using BusinessObjects.Dtos.Common;

namespace Services.Interfaces
{
    public interface IAccountService
    {
        Task<Account> GetAccountByEmailAsync(string email);
        Task<Account> GetAccountByIdAsync(int id);
        Task<IEnumerable<Account>> GetAllAccountsAsync();
        Task<bool> UpdateAccountAsync(int id, Account updatedAccount);
        //Task<bool> DeleteAccountAsync(int id);
        Task<Account> CreateExternalAccountAsync(ExternalAccountRegisterDto dto);
        
        // CRUD methods for Admin
        Task<PagedResultDto<AccountResponseDto>> GetAccountsAsync(AccountFilterDto filter);
        Task<AccountResponseDto?> GetAccountDetailAsync(int accountId);
        Task<(bool Success, string Message, AccountResponseDto? Account)> CreateAccountAsync(AccountCreateDto dto);
        Task<(bool Success, string Message)> UpdateAccountAsync(int accountId, AccountUpdateDto dto);
        Task<(bool Success, string Message)> DeleteAccountAsync(int accountId);
        Task<(bool Success, string Message)> ToggleAccountActiveStatusAsync(int accountId);

    }
}