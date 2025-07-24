using BusinessObjects.Domains;
using BusinessObjects.Dtos;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FavoritePostController : ControllerBase
    {
        private readonly IFavoritePostService _favoritePostService;

        public FavoritePostController(IFavoritePostService favoritePostService)
        {
            _favoritePostService = favoritePostService;
        }

        [HttpGet("account/{accountId}")]
        public async Task<IActionResult> GetByAccountId(int accountId)
        {
            var favorites = await _favoritePostService.GetByAccountIdAsync(accountId);
            var result = favorites.Select(f => new FavoritePostDto
            {
                FavoriteId = f.FavoriteId,
                PostId = f.PostId,
                AccountId = f.AccountId
            });

            return Ok(result);
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] FavoritePostDto dto)
        {
            var favorite = new FavoritePost
            {
                PostId = dto.PostId,
                AccountId = dto.AccountId,
                CreatedAt = DateTime.Now
            };

            await _favoritePostService.AddAsync(favorite);
            return Ok();
        }

        [HttpDelete("{favoriteId}")]
        public async Task<IActionResult> Delete(int favoriteId)
        {
            await _favoritePostService.DeleteAsync(favoriteId);
            return Ok();
        }
    }
}
