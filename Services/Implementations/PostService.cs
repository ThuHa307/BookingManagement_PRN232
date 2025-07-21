using BusinessObjects.Domains;
using RentNest.Core.DTO;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepository;
        public PostService(IPostRepository postRepository)
        {
            _postRepository = postRepository;
        }

        public async Task<List<Post>> GetAllPostsWithAccommodation()
        {
            return await _postRepository.GetAllPostsWithAccommodation();
        }

    }
}
