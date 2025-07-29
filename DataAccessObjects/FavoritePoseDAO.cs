using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
  using BusinessObjects.Domains;
using DataAccessObjects.DB;
using Microsoft.EntityFrameworkCore;

namespace DataAccessObjects
{
    


    

        public class FavoritePostDAO
        {
            public static async Task<List<FavoritePost>> GetAllAsync()
            {
                using var context = new RentNestSystemContext();
                return await context.FavoritePosts
                    .Include(fp => fp.Account)
                    .Include(fp => fp.Post)
                    .ToListAsync();
            }

            public static async Task<FavoritePost?> GetByIdAsync(int id)
            {
                using var context = new RentNestSystemContext();
                return await context.FavoritePosts
                    .Include(fp => fp.Account)
                    .Include(fp => fp.Post)
                    .FirstOrDefaultAsync(fp => fp.FavoriteId == id);
            }

            public static async Task<List<FavoritePost>> GetByAccountIdAsync(int accountId)
            {
                using var context = new RentNestSystemContext();
                return await context.FavoritePosts
                    .Include(fp => fp.Post)
                    .Where(fp => fp.AccountId == accountId)
                    .ToListAsync();
            }

            public static async Task AddAsync(FavoritePost favoritePost)
            {
                using var context = new RentNestSystemContext();
                await context.FavoritePosts.AddAsync(favoritePost);
                await context.SaveChangesAsync();
            }

            public static async Task DeleteAsync(int id)
            {
                using var context = new RentNestSystemContext();
                var fav = await context.FavoritePosts.FindAsync(id);
                if (fav != null)
                {
                    context.FavoritePosts.Remove(fav);
                    await context.SaveChangesAsync();
                }
            }
        }
    }

