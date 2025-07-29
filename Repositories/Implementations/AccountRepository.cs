using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Domains;
using BusinessObjects.Dtos;
using BusinessObjects.Dtos.Account;
using BusinessObjects.Dtos.Common;
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

        // CRUD methods for Admin
        public async Task<PagedResultDto<Account>> GetAccountsWithFilterAsync(AccountFilterDto filter)
        {
            var query = _context.Accounts.Include(a => a.UserProfile).AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.Email))
            {
                query = query.Where(a => a.Email.Contains(filter.Email));
            }

            if (!string.IsNullOrEmpty(filter.Username))
            {
                query = query.Where(a => a.Username != null && a.Username.Contains(filter.Username));
            }

            if (!string.IsNullOrEmpty(filter.Role))
            {
                query = query.Where(a => a.Role == filter.Role);
            }

            if (!string.IsNullOrEmpty(filter.IsActive))
            {
                query = query.Where(a => a.IsActive == filter.IsActive);
            }

            if (!string.IsNullOrEmpty(filter.AuthProvider))
            {
                query = query.Where(a => a.AuthProvider == filter.AuthProvider);
            }

            // Apply sorting
            query = filter.SortBy?.ToLower() switch
            {
                "email" => filter.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(a => a.Email)
                    : query.OrderBy(a => a.Email),
                "username" => filter.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(a => a.Username)
                    : query.OrderBy(a => a.Username),
                "role" => filter.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(a => a.Role)
                    : query.OrderBy(a => a.Role),
                "isactive" => filter.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(a => a.IsActive)
                    : query.OrderBy(a => a.IsActive),
                "createdat" => filter.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(a => a.CreatedAt)
                    : query.OrderBy(a => a.CreatedAt),
                _ => query.OrderByDescending(a => a.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResultDto<Account>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public async Task<Account?> GetAccountByIdWithProfileAsync(int accountId)
        {
            return await _context.Accounts
                .Include(a => a.UserProfile)
                .FirstOrDefaultAsync(a => a.AccountId == accountId);
        }

        public async Task<Account> CreateAccountAsync(AccountCreateDto dto)
        {
            var account = new Account
            {
                Email = dto.Email,
                Username = dto.Username,
                Password = dto.Password, // Should be hashed before calling this method
                Role = dto.Role,
                IsActive = dto.IsActive,
                AuthProvider = dto.AuthProvider.ToLower(),
                AuthProviderId = dto.AuthProviderId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TwoFactorEnabled = false
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();
            return account;
        }

        public async Task<bool> UpdateAccountAsync(int accountId, AccountUpdateDto dto)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == accountId);
            if (account == null)
                return false;

            account.Email = dto.Email;
            account.Username = dto.Username;
            account.Role = dto.Role;
            account.IsActive = dto.IsActive;
            account.UpdatedAt = DateTime.UtcNow;

            // If account is being disabled, clear refresh tokens
            if (dto.IsActive == "D")
            {
                account.RefreshToken = null;
                account.RefreshTokenExpiryTime = null;
            }

            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HasActivePostsAsync(int accountId)
        {
            // Kiểm tra xem tài khoản có bài đăng nào không (bao gồm cả bài đăng active và pending)
            return await _context.Posts
                .Where(p => p.AccountId == accountId)
                .AnyAsync();
        }

        public async Task<bool> DeleteAccountAsync(int accountId)
        {
            var account = await _context.Accounts
                .Include(a => a.UserProfile)
                .FirstOrDefaultAsync(a => a.AccountId == accountId);
            if (account == null)
                return false;

            // Delete related user profile if exists
            if (account.UserProfile != null)
            {
                _context.UserProfiles.Remove(account.UserProfile);
            }

            // Delete the account
            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleAccountActiveStatusAsync(int accountId)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == accountId);
            if (account == null)
                return false;

            // Toggle IsActive status
            account.IsActive = account.IsActive == "A" ? "D" : "A";
            account.UpdatedAt = DateTime.UtcNow;

            // If account is being disabled, clear refresh tokens
            if (account.IsActive == "D")
            {
                account.RefreshToken = null;
                account.RefreshTokenExpiryTime = null;
            }

            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<bool> IsEmailExistsForOtherAccountAsync(string email, int excludeAccountId)
        {
            return await _context.Accounts
                .AnyAsync(a => a.Email == email && a.AccountId != excludeAccountId);
        }

        public async Task<bool> IsUsernameExistsForOtherAccountAsync(string username, int excludeAccountId)
        {
            return await _context.Accounts
                .AnyAsync(a => a.Username == username && a.AccountId != excludeAccountId);
        } 
        
    }
}