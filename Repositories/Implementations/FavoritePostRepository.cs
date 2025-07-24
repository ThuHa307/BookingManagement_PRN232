using BusinessObjects.Domains;
using DataAccessObjects.DataAccessLayer.DAO;
using Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Implementations
{
    public class FavoritePostRepository : IFavoritePostRepository
    {
        public async Task<IEnumerable<FavoritePost>> GetAllAsync()
        {
            return await FavoritePostDAO.GetAllAsync();
        }

        public async Task<FavoritePost?> GetByIdAsync(int favoriteId)
        {
            return await FavoritePostDAO.GetByIdAsync(favoriteId);
        }

        public async Task<IEnumerable<FavoritePost>> GetByAccountIdAsync(int accountId)
        {
            return await FavoritePostDAO.GetByAccountIdAsync(accountId);
        }

        public async Task AddAsync(FavoritePost favoritePost)
        {
            await FavoritePostDAO.AddAsync(favoritePost);
        }

        public async Task DeleteAsync(int favoriteId)
        {
            await FavoritePostDAO.DeleteAsync(favoriteId);
        }

        public async Task SaveChangesAsync()
        {
            
        }
    }
}
