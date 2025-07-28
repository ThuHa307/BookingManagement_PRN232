using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Domains;
using DataAccessObjects.DB;
using Microsoft.EntityFrameworkCore;
using RentNest.Infrastructure.DataAccess;

namespace DataAccessObjects
{
    public class UserProfileDAO : BaseDAO<UserProfile>
    {

        public UserProfileDAO(RentNestSystemContext context) : base(context) { }

        public async Task<UserProfile?> GetUserProfileByAccountIdAsync(int accountId)
        {
            return await _context.UserProfiles
                .Include(up => up.Account)
                .FirstOrDefaultAsync(up => up.AccountId == accountId);
        }

        public async Task<UserProfile?> GetUserProfileByProfileIdAsync(int profileId)
        {
            return await _context.UserProfiles
                .Include(up => up.Account)
                .FirstOrDefaultAsync(up => up.ProfileId == profileId);
        }

        public async Task<UserProfile> CreateUserProfileAsync(UserProfile userProfile)
        {
            _context.UserProfiles.Add(userProfile);
            await _context.SaveChangesAsync();
            return userProfile;
        }

        public async Task<UserProfile> UpdateUserProfileAsync(UserProfile userProfile)
        {
            _context.UserProfiles.Update(userProfile);
            await _context.SaveChangesAsync();
            return userProfile;
        }

        public async Task<bool> DeleteUserProfileAsync(int profileId)
        {
            var userProfile = await _context.UserProfiles.FindAsync(profileId);
            if (userProfile == null)
                return false;

            _context.UserProfiles.Remove(userProfile);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UserProfileExistsAsync(int accountId)
        {
            return await _context.UserProfiles.AnyAsync(up => up.AccountId == accountId);
        }

        public async Task<List<UserProfile>> GetAllUserProfilesAsync()
        {
            return await _context.UserProfiles
                .Include(up => up.Account)
                .ToListAsync();
        }
    }
}
