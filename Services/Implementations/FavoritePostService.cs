using BusinessObjects.Domains;
using Repositories.Interfaces;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class FavoritePostService : IFavoritePostService
    {
        private readonly IFavoritePostRepository _favoritePostRepository;

        public FavoritePostService(IFavoritePostRepository favoritePostRepository)
        {
            _favoritePostRepository = favoritePostRepository;
        }

        public async Task<IEnumerable<FavoritePost>> GetAllAsync()
        {
            return await _favoritePostRepository.GetAllAsync();
        }

        public async Task<FavoritePost?> GetByIdAsync(int id)
        {
            return await _favoritePostRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<FavoritePost>> GetByAccountIdAsync(int accountId)
        {
            return await _favoritePostRepository.GetByAccountIdAsync(accountId);
        }

        public async Task AddAsync(FavoritePost favoritePost)
        {
            await _favoritePostRepository.AddAsync(favoritePost);
            await _favoritePostRepository.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            await _favoritePostRepository.DeleteAsync(id);
            await _favoritePostRepository.SaveChangesAsync();
        }
    }
}
