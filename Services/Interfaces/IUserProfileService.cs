using BusinessObjects.Dtos;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IUserProfileService
    {
        Task<UserProfileDto?> GetUserProfileAsync(int accountId);
        Task<UserProfileDto> CreateOrUpdateUserProfileAsync(int accountId, UserProfileDto userProfileDto);
        Task<bool> DeleteUserProfileAsync(int accountId);
        Task<string?> UploadAvatarAsync(int accountId, IFormFile avatar);
        Task<bool> DeleteAvatarAsync(int accountId);
        Task<List<UserProfileDto>> GetAllUserProfilesAsync();
    }
}
