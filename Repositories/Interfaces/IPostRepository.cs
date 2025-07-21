using BusinessObjects.Domains;
using RentNest.Core.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IPostRepository
    {
        Task<List<Post>> GetAllPostsWithAccommodation();
    }
}
