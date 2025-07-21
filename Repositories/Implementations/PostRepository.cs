using Microsoft.EntityFrameworkCore;
using RentNest.Core.DTO;
using RentNest.Infrastructure.DataAccess;
using Repositories.Interfaces;
using RentNest.Core.UtilHelper;
using System.Security.Claims;
using Microsoft.Identity.Client;
using RentNest.Core.Enums;
using BusinessObjects.Domains;


namespace Repositories.Implementations
{
    public class PostRepository : IPostRepository
    {
		private readonly PostDAO _postDAO;

		public PostRepository(PostDAO postDAO)
		{
			_postDAO = postDAO;
		}

		public async Task<List<Post>> GetAllPostsWithAccommodation()
		{
			return await _postDAO.GetAllPostsWithAccommodation();
		}
	}
}
