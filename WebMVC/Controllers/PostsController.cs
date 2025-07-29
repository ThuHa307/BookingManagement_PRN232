using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Dtos.ChatBot;
using BusinessObjects.Dtos.Post;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RentNest.Core.DTO;
using RentNest.Core.Enums;
using RentNest.Core.Model.Momo;
using RentNest.Core.UtilHelper;
using WebMVC.API;

namespace WebMVC.Controllers
{
    [Route("[controller]")]
    public class PostsController : Controller
    {
        private readonly PostApiService _postApiService;
        private readonly ILogger<PostsController> _logger; // Thêm biến logger


        public PostsController(PostApiService postApiService, ILogger<PostsController> logger) // Inject ILogger
        {
            _postApiService = postApiService;
            _logger = logger; // Gán logger
        }

        // API endpoints (proxied through PostApiService)
        [HttpGet("/api/v1/package-types/{timeUnitId}")]
        public async Task<IActionResult> GetPackageTypesByTimeUnit(int timeUnitId)
        {
            var result = await _postApiService.GetPackageTypesByTimeUnit(timeUnitId);
            return Ok(result);
        }

        [HttpGet("/api/v1/durations")]
        public async Task<IActionResult> GetDurations(int timeUnitId, int packageTypeId)
        {
            var result = await _postApiService.GetDurations(timeUnitId, packageTypeId);
            return Ok(result);
        }

        [HttpPost]
        [Route("/api/v1/get-pricing")]
        public async Task<IActionResult> GetPricingId([FromBody] PricingLookupDto dto)
        {
            var pricingId = await _postApiService.GetPricingId(dto);

            if (pricingId == null)
                return NotFound(new { message = "Không tìm thấy gói tương ứng." });

            return Ok(new { pricingId });
        }


        // MVC Views and Actions
        [Route("/nguoi-cho-thue/dang-tin")]
        [HttpGet]
        public async Task<IActionResult> Post()
        {
            var currentUserId = HttpContext.Session.GetString("UserId");
            var model = await _postApiService.GetCreatePostInitialData(Int32.Parse(currentUserId ?? "0"));
            return View("User/CreatePost", model);
        }


        [Route("/nguoi-cho-thue/dang-tin")]
        [HttpPost]
        public async Task<IActionResult> Post_Landlord([FromForm] LandlordPostDto dto)
        {
            var userId = HttpContext.Session.GetString("UserId");
            dto.OwnerId = Int32.Parse(userId); // Still get OwnerId from MVC side for form submission

            var (success, postId, amount) = await _postApiService.CreatePost(dto);

            return Json(new
            {
                success = success,
                postId = postId,
                amount = dto.TotalPrice,
            });
        }


        [HttpPost("GeneratePostWithAI")]
        public async Task<IActionResult> GeneratePostWithAI([FromBody] PostDataAIDto model)
        {
            var content = await _postApiService.GeneratePostWithAI(model);
            return Ok(new { content });
        }

        [HttpGet("/quan-ly-tin")]
        public async Task<IActionResult> ManagePost([FromQuery] string status = null)
        {
            if (string.IsNullOrEmpty(status))
            {
                status = PostStatusHelper.ToDbValue(PostStatus.Pending);
            }

            var accountId = Int32.TryParse(HttpContext.Session.GetString("UserId"), out var parsedId) ? parsedId : 0;

            if (accountId == 0) return Unauthorized(); // Or handle error appropriately

            var (filteredPosts, statusCounts) = await _postApiService.GetManagePosts(status, accountId);

            // You'd need to fetch user profile details separately or include them in ManagePostViewModel if needed for ViewBag.
            // For demonstration, let's assume ManagePostViewModel now contains enough data or we will fetch them if needed.

            // Example of how you might get account name and avatar if not in ManagePostViewModel directly:
            // This would typically come from an API call to an account service.
            // For now, let's just use placeholder or derive from existing data if possible.
            // If the API for manage posts returns account info with each post, you can get it from there.
            var firstPost = filteredPosts.FirstOrDefault(); // Assuming all posts belong to the same user
            ViewBag.AccountName = firstPost?.AccountName ?? ""; // Assuming ManagePostViewModel has AccountName
            ViewBag.AvatarUrl = firstPost?.AvatarUrl ?? "/images/default-avatar.jpg"; // Assuming ManagePostViewModel has AvatarUrl

            ViewBag.CurrentStatus = status;
            ViewBag.StatusCounts = statusCounts;

            return View("User/ManagePost", filteredPosts);
        }

        


    }
}