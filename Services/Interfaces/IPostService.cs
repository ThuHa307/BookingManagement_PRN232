using BusinessObjects.Domains;
using RentNest.Core.DTO;

namespace Services.Interfaces
{
    public interface IPostService
    {
        Task<List<Post>> GetAllPostsWithAccommodation();
    }
}
