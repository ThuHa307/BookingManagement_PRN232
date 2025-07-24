using BusinessObjects.Dtos;
using Microsoft.AspNetCore.Mvc;
using WebMVC.API;

namespace WebMVC.Controllers
{
    public class FavoritePostController : Controller
    {
        private readonly FavoritePostApiService _favoriteService;

        public FavoritePostController(FavoritePostApiService favoriteService)
        {
            _favoriteService = favoriteService;
        }

        public async Task<IActionResult> Index()
        {
            int accountId = HttpContext.Session.GetInt32("AccountId") ?? 0;
            var favorites = await _favoriteService.GetByAccountIdAsync(accountId);
            return View(favorites); 
        }

        [HttpPost]

        public async Task<IActionResult> Add(int postId)
        {
            int accountId = HttpContext.Session.GetInt32("AccountId") ?? 0;

            var dto = new FavoritePostDto
            {
                PostId = postId,
                AccountId = accountId
            };

            var success = await _favoriteService.AddAsync(dto);
            if (success)
                TempData["Success"] = "Đã thêm vào yêu thích.";
            else
                TempData["Error"] = "Bài viết đã được thêm trước đó.";
            return RedirectToAction("Index", "Accommodations"); 
        }


        public async Task<IActionResult> Delete(int postId)
        {
            var accountId = HttpContext.Session.GetInt32("AccountId") ?? 0;
            var favorites = await _favoriteService.GetByAccountIdAsync(accountId);
            var fav = favorites.FirstOrDefault(f => f.PostId == postId);

            if (fav != null)
            {
                await _favoriteService.DeleteAsync(fav.FavoriteId);
            }

            // Quay về trang đã gọi request này
            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
            {
                return Redirect(referer);
            }

            return RedirectToAction("Index", "Accommodations");
        }
    }
}
