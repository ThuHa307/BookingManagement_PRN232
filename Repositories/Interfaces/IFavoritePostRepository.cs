using BusinessObjects.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IFavoritePostRepository
    {
        Task<IEnumerable<FavoritePost>> GetAllAsync();
        Task<FavoritePost?> GetByIdAsync(int favoriteId);
        Task<IEnumerable<FavoritePost>> GetByAccountIdAsync(int accountId);
        Task AddAsync(FavoritePost favoritePost);
        Task DeleteAsync(int favoriteId);
        Task SaveChangesAsync();
    }
}
