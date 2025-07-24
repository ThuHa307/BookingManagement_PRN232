using BusinessObjects.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IFavoritePostService
    {
        Task<IEnumerable<FavoritePost>> GetAllAsync();
        Task<FavoritePost?> GetByIdAsync(int id);
        Task<IEnumerable<FavoritePost>> GetByAccountIdAsync(int accountId);
        Task AddAsync(FavoritePost favoritePost);
        Task DeleteAsync(int id);
    }
}
