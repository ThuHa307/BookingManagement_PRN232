using BusinessObjects.Domains;
using DataAccessObjects;
using Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Implementations
{
    public class UserProfileRepository : IUserProfileRepository
    {
        private readonly UserProfileDAO _userProfileDAO;

        public UserProfileRepository(UserProfileDAO userProfileDAO)
        {
            _userProfileDAO = userProfileDAO;
        }

        public async Task<UserProfile?> GetUserProfileByAccountIdAsync(int accountId)
        {
            return await _userProfileDAO.GetUserProfileByAccountIdAsync(accountId);
        }

        public async Task<UserProfile?> GetUserProfileByProfileIdAsync(int profileId)
        {
            return await _userProfileDAO.GetUserProfileByProfileIdAsync(profileId);
        }

        public async Task<UserProfile> CreateUserProfileAsync(UserProfile userProfile)
        {
            userProfile.CreatedAt = DateTime.UtcNow;
            userProfile.UpdatedAt = DateTime.UtcNow;
            return await _userProfileDAO.CreateUserProfileAsync(userProfile);
        }

        public async Task<UserProfile> UpdateUserProfileAsync(UserProfile userProfile)
        {
            userProfile.UpdatedAt = DateTime.UtcNow;
            return await _userProfileDAO.UpdateUserProfileAsync(userProfile);
        }

        public async Task<bool> DeleteUserProfileAsync(int profileId)
        {
            return await _userProfileDAO.DeleteUserProfileAsync(profileId);
        }

        public async Task<bool> UserProfileExistsAsync(int accountId)
        {
            return await _userProfileDAO.UserProfileExistsAsync(accountId);
        }

        public async Task<List<UserProfile>> GetAllUserProfilesAsync()
        {
            return await _userProfileDAO.GetAllUserProfilesAsync();
        }
    }
}
