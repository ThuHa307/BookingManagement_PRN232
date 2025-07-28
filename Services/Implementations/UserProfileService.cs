using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Domains;
using BusinessObjects.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations
{
    public class UserProfileService : IUserProfileService
    {
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly ILogger<UserProfileService> _logger;

        public UserProfileService(
            IUserProfileRepository userProfileRepository,
            IAccountRepository accountRepository,
            IHostEnvironment hostEnvironment,
            ILogger<UserProfileService> logger)
        {
            _userProfileRepository = userProfileRepository;
            _accountRepository = accountRepository;
            _hostEnvironment = hostEnvironment;
            _logger = logger;
        }

        public async Task<UserProfileDto?> GetUserProfileAsync(int accountId)
        {
            try
            {
                // Kiểm tra account có tồn tại không
                var account = await _accountRepository.GetByIdAsync(accountId);
                if (account == null)
                {
                    return null;
                }

                // Lấy profile từ database
                var userProfile = await _userProfileRepository.GetUserProfileByAccountIdAsync(accountId);

                // Nếu account mới tạo chưa có profile thì trả về null
                if (userProfile == null)
                {
                    return null;
                }

                // Map entity sang DTO
                return MapToDto(userProfile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile for account {AccountId}", accountId);
                throw;
            }
        }

        public async Task<UserProfileDto> CreateOrUpdateUserProfileAsync(int accountId, UserProfileDto userProfileDto)
        {
            try
            {
                // Kiểm tra account có tồn tại không
                var account = await _accountRepository.GetByIdAsync(accountId);
                if (account == null)
                {
                    throw new ArgumentException("Account không tồn tại", nameof(accountId));
                }

                var existingProfile = await _userProfileRepository.GetUserProfileByAccountIdAsync(accountId);

                if (existingProfile == null)
                {
                    // Tạo mới profile
                    var newProfile = new UserProfile
                    {
                        AccountId = accountId,
                        FirstName = userProfileDto.FirstName,
                        LastName = userProfileDto.LastName,
                        PhoneNumber = userProfileDto.PhoneNumber,
                        Gender = userProfileDto.Gender,
                        DateOfBirth = userProfileDto.DateOfBirth,
                        AvatarUrl = userProfileDto.AvatarUrl,
                        Occupation = userProfileDto.Occupation,
                        Address = userProfileDto.Address
                    };

                    var createdProfile = await _userProfileRepository.CreateUserProfileAsync(newProfile);
                    return MapToDto(createdProfile);
                }
                else
                {
                    // Cập nhật profile hiện có
                    existingProfile.FirstName = userProfileDto.FirstName;
                    existingProfile.LastName = userProfileDto.LastName;
                    existingProfile.PhoneNumber = userProfileDto.PhoneNumber;
                    existingProfile.Gender = userProfileDto.Gender;
                    existingProfile.DateOfBirth = userProfileDto.DateOfBirth;
                    existingProfile.Occupation = userProfileDto.Occupation;
                    existingProfile.Address = userProfileDto.Address;

                    // Chỉ cập nhật AvatarUrl nếu có giá trị mới
                    if (!string.IsNullOrEmpty(userProfileDto.AvatarUrl))
                    {
                        existingProfile.AvatarUrl = userProfileDto.AvatarUrl;
                    }

                    var updatedProfile = await _userProfileRepository.UpdateUserProfileAsync(existingProfile);
                    return MapToDto(updatedProfile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating user profile for account {AccountId}", accountId);
                throw;
            }
        }

        public async Task<bool> DeleteUserProfileAsync(int accountId)
        {
            try
            {
                var userProfile = await _userProfileRepository.GetUserProfileByAccountIdAsync(accountId);
                if (userProfile == null)
                {
                    return false;
                }

                // Xóa avatar file nếu có
                if (!string.IsNullOrEmpty(userProfile.AvatarUrl))
                {
                    await DeleteAvatarFileAsync(userProfile.AvatarUrl);
                }

                return await _userProfileRepository.DeleteUserProfileAsync(userProfile.ProfileId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user profile for account {AccountId}", accountId);
                throw;
            }
        }

        public async Task<string?> UploadAvatarAsync(int accountId, IFormFile avatar)
        {
            try
            {
                if (avatar == null || avatar.Length == 0)
                {
                    throw new ArgumentException("File avatar không hợp lệ");
                }

                // Kiểm tra định dạng file
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(avatar.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    throw new ArgumentException("Chỉ chấp nhận file ảnh (.jpg, .jpeg, .png, .gif)");
                }

                // Kiểm tra kích thước file (5MB)
                if (avatar.Length > 5 * 1024 * 1024)
                {
                    throw new ArgumentException("Kích thước file không được vượt quá 5MB");
                }

                // Tạo thư mục uploads nếu chưa có
                var uploadsFolder = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot", "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Tạo tên file unique
                var fileName = $"{accountId}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Xóa avatar cũ nếu có
                var existingProfile = await _userProfileRepository.GetUserProfileByAccountIdAsync(accountId);
                if (existingProfile?.AvatarUrl != null)
                {
                    await DeleteAvatarFileAsync(existingProfile.AvatarUrl);
                }

                // Lưu file mới
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatar.CopyToAsync(stream);
                }

                // Tạo URL relative
                var avatarUrl = $"/uploads/avatars/{fileName}";

                // Cập nhật hoặc tạo profile với avatar URL
                if (existingProfile == null)
                {
                    var newProfile = new UserProfile
                    {
                        AccountId = accountId,
                        AvatarUrl = avatarUrl
                    };
                    await _userProfileRepository.CreateUserProfileAsync(newProfile);
                }
                else
                {
                    existingProfile.AvatarUrl = avatarUrl;
                    await _userProfileRepository.UpdateUserProfileAsync(existingProfile);
                }

                return avatarUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar for account {AccountId}", accountId);
                throw;
            }
        }

        public async Task<bool> DeleteAvatarAsync(int accountId)
        {
            try
            {
                var userProfile = await _userProfileRepository.GetUserProfileByAccountIdAsync(accountId);
                if (userProfile?.AvatarUrl == null)
                {
                    return false;
                }

                // Xóa file avatar
                await DeleteAvatarFileAsync(userProfile.AvatarUrl);

                // Cập nhật database
                userProfile.AvatarUrl = null;
                await _userProfileRepository.UpdateUserProfileAsync(userProfile);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting avatar for account {AccountId}", accountId);
                throw;
            }
        }

        public async Task<List<UserProfileDto>> GetAllUserProfilesAsync()
        {
            try
            {
                var userProfiles = await _userProfileRepository.GetAllUserProfilesAsync();
                return userProfiles.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all user profiles");
                throw;
            }
        }

        private async Task DeleteAvatarFileAsync(string avatarUrl)
        {
            try
            {
                var fileName = Path.GetFileName(avatarUrl);
                var filePath = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot", "uploads", "avatars", fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not delete avatar file: {AvatarUrl}", avatarUrl);
            }
        }

        private UserProfileDto MapToDto(UserProfile userProfile)
        {
            return new UserProfileDto
            {
                FirstName = userProfile.FirstName,
                LastName = userProfile.LastName,
                PhoneNumber = userProfile.PhoneNumber,
                Gender = userProfile.Gender,
                DateOfBirth = userProfile.DateOfBirth,
                AvatarUrl = userProfile.AvatarUrl,
                Occupation = userProfile.Occupation,
                Address = userProfile.Address,
                CreatedAt = userProfile.CreatedAt,
                UpdatedAt = userProfile.UpdatedAt
            };
        }
    }
}