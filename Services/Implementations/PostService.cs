using BusinessObjects.Domains;
using BusinessObjects.Dtos.ChatBot;
using RentNest.Core.DTO;
using Repositories.Interfaces;
using Services.Interfaces;
using System.Net.Http;

namespace Services.Implementations
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepository;
        public PostService(IPostRepository postRepository)
        {
            _postRepository = postRepository;
        }

        public async Task<List<Post>> GetAllPostsByUserAsync(int accountId)
        {
            return await _postRepository.GetAllPostsByUserAsync(accountId);
        }

        public async Task<List<Post>> GetAllPostsWithAccommodation()
        {
            return await _postRepository.GetAllPostsWithAccommodation();
        }

        public async Task<Post?> GetPostDetailWithAccommodationDetailAsync(int postId)
        {
            return await _postRepository.GetPostDetailWithAccommodationDetailAsync(postId);
        }

        public async Task<List<Post>> GetTopVipPostsAsync()
        {
            return await _postRepository.GetTopVipPostsAsync();
        }

        public async Task<int> SavePost(LandlordPostDto dto)
        {
            return await _postRepository.SavePost(dto);
        }

        public async Task<List<Comment>> GetCommentsByPostId(int postId)
        {
            // Assuming there's a method in the repository to get comments by post ID
            return await _postRepository.GetCommentsByPostId(postId);
        }

        public async Task<List<Post>> GetAllPosts()
        {
            return await _postRepository.GetAllPosts();

        }

    }
}
