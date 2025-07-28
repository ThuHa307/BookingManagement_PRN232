using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Domains;
using BusinessObjects.Dtos;
using DataAccessObjects;
using DataAccessObjects.DB;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class AccountRepository : GenericRepository<Account>, IAccountRepository
    {
        private readonly AccountDAO _accountDAO;
        private readonly UserProfileDAO _userProfileDAO;

        public AccountRepository(RentNestSystemContext context, AccountDAO accountDAO, UserProfileDAO userProfileDAO) : base(context)
        {
            _accountDAO = accountDAO;
            _userProfileDAO = userProfileDAO;
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

        public async Task<UserProfile?> GetProfileByAccountIdAsync(int accountId)
        {
            return await _userProfileDAO.GetUserProfileByAccountIdAsync(accountId);
        }

        public async Task UpdateProfileAsync(UserProfile profile)
        {
            await _userProfileDAO.UpdateUserProfileAsync(profile);
        }

        public async Task UpdateAvatarAsync(UserProfile profile, string avatarUrl)
        {
            profile.AvatarUrl = avatarUrl;
            await _userProfileDAO.UpdateUserProfileAsync(profile);
        }


        public async Task<Account?> GetAccountByEmailAsync(string email)
        {
            return await _accountDAO.GetAccountByEmailAsync(email);
        }

        public async Task Update(Account account)
        {
            await _accountDAO.UpdateAsync(account);
        }

        public async Task<Account> CreateExternalAccountAsync(ExternalAccountRegisterDto dto)
        {
            var account = new Account
            {
                Email = dto.Email,
                AuthProvider = dto.AuthProvider.ToLower(),
                AuthProviderId = dto.AuthProviderId,
                Role = dto.Role,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _accountDAO.AddAsync(account);

                var userProfile = new UserProfile
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Address = dto.Address,
                    PhoneNumber = dto.PhoneNumber,
                    CreatedAt = DateTime.UtcNow,
                    AccountId = account.AccountId
                };

                await _userProfileDAO.AddAsync(userProfile);
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi tạo tài khoản: " + ex.Message);
            }
            return account;
        }


        public async Task SetUserOnlineAsync(int userId, bool isOnline)
        {
            var user = await _accountDAO.GetByIdAsync(userId);
            if (user != null)
            {
                user.IsOnline = isOnline;
                await _accountDAO.UpdateAsync(user);
            }
        }

        public async Task UpdateLastActiveAsync(int userId)
        {
            var user = await _accountDAO.GetByIdAsync(userId);
            if (user != null)
            {
                user.LastActiveAt = DateTime.UtcNow.AddHours(7);
                await _accountDAO.UpdateAsync(user);
            }
        }

        public async Task<Account> GetAccountById(int accountId)
        {
            return await _accountDAO.GetByIdAsync(accountId);
        }

        public async Task<bool> CheckEmailExistsAsync(string email)
        {
            return await _accountDAO.CheckEmailExistsAsync(email);
        }

        public async Task<bool> CheckUsernameExistsAsync(string username)
        {
            return await _accountDAO.CheckEmailExistsAsync(username);
        }

        public async Task<Account?> GetAccountByEmailOrUsernameAsync(string input)
        {
            return await _accountDAO.GetAccountByEmailOrUsernameAsync(input);
        }
    }
}