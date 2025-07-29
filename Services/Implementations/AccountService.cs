using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Domains;
using BusinessObjects.Dtos;
using BusinessObjects.Dtos.Account;
using BusinessObjects.Dtos.Common;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IPasswordHasherCustom _passwordHasher;
        public AccountService(IAccountRepository accountRepository, IPasswordHasherCustom passwordHasher)
        {
            _accountRepository = accountRepository;
            _passwordHasher = passwordHasher;
        }
        public async Task<Account> GetAccountByEmailAsync(string email)
        {
            return await _accountRepository.GetSystemAccountByEmailAsync(email);
        }
        public async Task<bool> UpdateAccountAsync(int id, Account updatedAccount)
        {
            var acc = await _accountRepository.GetByIdAsync(id);
            if (acc == null) return false;
            acc.Email = updatedAccount.Email;
            acc.Username = updatedAccount.Username;
            // ... các trường khác
            await _accountRepository.UpdateAsync(acc);
            await _accountRepository.SaveChangesAsync();
            return true;
        }

        /*public async Task<bool> DeleteAccountAsync(int id)
        {
            var acc = await _accountRepository.GetByIdAsync(id);
            if (acc == null) return false;
            _accountRepository.Delete(acc);
            await _accountRepository.SaveChangesAsync();
            return true;
        }*/

        public async Task<IEnumerable<Account>> GetAllAccountsAsync()
        => await _accountRepository.GetAllAsync();

        public async Task<Account> GetAccountByIdAsync(int id)
        {
            return await _accountRepository.GetByIdAsync(id);
        }

        public async Task<Account> CreateExternalAccountAsync(ExternalAccountRegisterDto dto)
        {
            return await _accountRepository.CreateExternalAccountAsync(dto);
        }

        // CRUD methods for Admin
        public async Task<PagedResultDto<AccountResponseDto>> GetAccountsAsync(AccountFilterDto filter)
        {
            var result = await _accountRepository.GetAccountsWithFilterAsync(filter);

            var accountDtos = result.Items.Select(account => new AccountResponseDto
            {
                AccountId = account.AccountId,
                Username = account.Username,
                Email = account.Email,
                IsActive = account.IsActive,
                AuthProvider = account.AuthProvider,
                AuthProviderId = account.AuthProviderId,
                IsOnline = account.IsOnline,
                LastActiveAt = account.LastActiveAt,
                CreatedAt = account.CreatedAt,
                UpdatedAt = account.UpdatedAt,
                Role = account.Role,
                TwoFactorEnabled = account.TwoFactorEnabled,
                UserProfile = account.UserProfile != null ? new UserProfileResponseDto
                {
                    ProfileId = account.UserProfile.ProfileId,
                    FirstName = account.UserProfile.FirstName,
                    LastName = account.UserProfile.LastName,
                    Address = account.UserProfile.Address,
                    PhoneNumber = account.UserProfile.PhoneNumber,
                    AvatarUrl = account.UserProfile.AvatarUrl,
                    CreatedAt = account.UserProfile.CreatedAt,
                    UpdatedAt = account.UserProfile.UpdatedAt
                } : null
            });

            return new PagedResultDto<AccountResponseDto>
            {
                Items = accountDtos,
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            };
        }

        public async Task<AccountResponseDto?> GetAccountDetailAsync(int accountId)
        {
            var account = await _accountRepository.GetAccountByIdWithProfileAsync(accountId);
            if (account == null)
                return null;

            return new AccountResponseDto
            {
                AccountId = account.AccountId,
                Username = account.Username,
                Email = account.Email,
                IsActive = account.IsActive,
                AuthProvider = account.AuthProvider,
                AuthProviderId = account.AuthProviderId,
                IsOnline = account.IsOnline,
                LastActiveAt = account.LastActiveAt,
                CreatedAt = account.CreatedAt,
                UpdatedAt = account.UpdatedAt,
                Role = account.Role,
                TwoFactorEnabled = account.TwoFactorEnabled,
                UserProfile = account.UserProfile != null ? new UserProfileResponseDto
                {
                    ProfileId = account.UserProfile.ProfileId,
                    FirstName = account.UserProfile.FirstName,
                    LastName = account.UserProfile.LastName,
                    Address = account.UserProfile.Address,
                    PhoneNumber = account.UserProfile.PhoneNumber,
                    AvatarUrl = account.UserProfile.AvatarUrl,
                    CreatedAt = account.UserProfile.CreatedAt,
                    UpdatedAt = account.UserProfile.UpdatedAt
                } : null
            };
        }

        public async Task<(bool Success, string Message, AccountResponseDto? Account)> CreateAccountAsync(AccountCreateDto dto)
        {
            try
            {
                // Check if email already exists
                if (await _accountRepository.CheckEmailExistsAsync(dto.Email))
                {
                    return (false, "Email đã được sử dụng bởi tài khoản khác.", null);
                }

                // Check if username already exists (if provided)
                if (!string.IsNullOrEmpty(dto.Username) &&
                    await _accountRepository.CheckUsernameExistsAsync(dto.Username))
                {
                    return (false, "Username đã được sử dụng bởi tài khoản khác.", null);
                }

                // Hash password
                var hashedPassword = _passwordHasher.HashPassword(dto.Password);
                dto.Password = hashedPassword;

                // Create account
                var createdAccount = await _accountRepository.CreateAccountAsync(dto);

                // Get account with profile for response
                var accountWithProfile = await _accountRepository.GetAccountByIdWithProfileAsync(createdAccount.AccountId);

                var accountResponse = new AccountResponseDto
                {
                    AccountId = accountWithProfile.AccountId,
                    Username = accountWithProfile.Username,
                    Email = accountWithProfile.Email,
                    IsActive = accountWithProfile.IsActive,
                    AuthProvider = accountWithProfile.AuthProvider,
                    AuthProviderId = accountWithProfile.AuthProviderId,
                    IsOnline = accountWithProfile.IsOnline,
                    LastActiveAt = accountWithProfile.LastActiveAt,
                    CreatedAt = accountWithProfile.CreatedAt,
                    UpdatedAt = accountWithProfile.UpdatedAt,
                    Role = accountWithProfile.Role,
                    TwoFactorEnabled = accountWithProfile.TwoFactorEnabled
                };

                return (true, "Tạo tài khoản thành công.", accountResponse);
            }
            catch (Exception ex)
            {
                return (false, $"Có lỗi xảy ra khi tạo tài khoản: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> UpdateAccountAsync(int accountId, AccountUpdateDto dto)
        {
            try
            {
                // Check if account exists
                var existingAccount = await _accountRepository.GetByIdAsync(accountId);
                if (existingAccount == null)
                {
                    return (false, "Không tìm thấy tài khoản.");
                }

                // Check if email is being used by another account
                if (await _accountRepository.IsEmailExistsForOtherAccountAsync(dto.Email, accountId))
                {
                    return (false, "Email đã được sử dụng bởi tài khoản khác.");
                }

                // Check if username is being used by another account (if provided)
                if (!string.IsNullOrEmpty(dto.Username) &&
                    await _accountRepository.IsUsernameExistsForOtherAccountAsync(dto.Username, accountId))
                {
                    return (false, "Username đã được sử dụng bởi tài khoản khác.");
                }

                // Update account
                var updated = await _accountRepository.UpdateAccountAsync(accountId, dto);
                if (!updated)
                {
                    return (false, "Không thể cập nhật tài khoản.");
                }

                return (true, "Cập nhật tài khoản thành công.");
            }
            catch (Exception ex)
            {
                return (false, $"Có lỗi xảy ra khi cập nhật tài khoản: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteAccountAsync(int accountId)
        {
            try
            {
                // Check if account exists
                var existingAccount = await _accountRepository.GetByIdAsync(accountId);
                if (existingAccount == null)
                {
                    return (false, "Không tìm thấy tài khoản.");
                }

                // Kiểm tra nếu là chủ nhà (Landlord) - Role = "L"
                if (existingAccount.Role == "L")
                {
                    // Kiểm tra xem chủ nhà có bài đăng nào không
                    var hasActivePosts = await _accountRepository.HasActivePostsAsync(accountId);
                    if (hasActivePosts)
                    {
                        return (false, "Không thể xóa tài khoản chủ nhà này vì đã có bài đăng. Vui lòng xóa tất cả bài đăng trước khi xóa tài khoản.");
                    }
                }

                // Hard delete account
                var deleted = await _accountRepository.DeleteAccountAsync(accountId);
                if (!deleted)
                {
                    return (false, "Không thể xóa tài khoản.");
                }

                return (true, "Xóa tài khoản thành công.");
            }
            catch (Exception ex)
            {
                return (false, $"Có lỗi xảy ra khi xóa tài khoản: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ToggleAccountActiveStatusAsync(int accountId)
        {
            try
            {
                // Check if account exists
                var existingAccount = await _accountRepository.GetByIdAsync(accountId);
                if (existingAccount == null)
                {
                    return (false, "Không tìm thấy tài khoản.");
                }

                // Toggle account status
                var toggled = await _accountRepository.ToggleAccountActiveStatusAsync(accountId);
                if (!toggled)
                {
                    return (false, "Không thể thay đổi trạng thái tài khoản.");
                }

                return (true, "Thay đổi trạng thái tài khoản thành công.");
            }
            catch (Exception ex)
            {
                return (false, $"Có lỗi xảy ra khi thay đổi trạng thái tài khoản: {ex.Message}");
            }
        }
    }

}
