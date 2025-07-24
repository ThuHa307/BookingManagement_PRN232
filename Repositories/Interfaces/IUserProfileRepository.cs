using BusinessObjects.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IUserProfileRepository
    {
        Task<UserProfile?> GetUserProfileByAccountIdAsync(int accountId);
        Task<UserProfile?> GetUserProfileByProfileIdAsync(int profileId);
        Task<UserProfile> CreateUserProfileAsync(UserProfile userProfile);
        Task<UserProfile> UpdateUserProfileAsync(UserProfile userProfile);
        Task<bool> DeleteUserProfileAsync(int profileId);
        Task<bool> UserProfileExistsAsync(int accountId);
        Task<List<UserProfile>> GetAllUserProfilesAsync();
    }
}
