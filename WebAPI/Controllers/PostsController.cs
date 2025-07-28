using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BusinessObjects.Dtos.ChatBot;
using BusinessObjects.Dtos.Post;
using Microsoft.AspNetCore.Mvc;
using RentNest.Core.DTO;
using RentNest.Core.Enums;
using RentNest.Core.UtilHelper;
using Services.Interfaces;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/v1/posts")]
    public class PostsController : ControllerBase
    {
        private readonly IAzureOpenAIService _azureOpenAIService;
        private readonly IAccommodationTypeService _accommodationTypeService;
        private readonly IAmenitiesSerivce _amenitiesService;
        private readonly ITimeUnitPackageService _timeUnitPackageService;
        private readonly IPackagePricingService _packagePricingService;
        private readonly IPostService _postService;
        private readonly IAccountService _accountService;
        public PostsController(IAzureOpenAIService azureOpenAIService, IAccommodationTypeService accommodationTypeService, IAmenitiesSerivce amenitiesService, ITimeUnitPackageService timeUnitPackageService,
        IPostService postService, IPackagePricingService packagePricingService, IAccountService accountService)
        {
            _azureOpenAIService = azureOpenAIService;
            _accommodationTypeService = accommodationTypeService;
            _amenitiesService = amenitiesService;
            _timeUnitPackageService = timeUnitPackageService;
            _packagePricingService = packagePricingService;
            _postService = postService;
            _accountService = accountService;
        }
        // Pricing related APIs
        [HttpGet("package-types/{timeUnitId}")]
        public async Task<IActionResult> GetPackageTypesByTimeUnit(int timeUnitId)
        {
            var result = await _packagePricingService.GetPackageTypes(timeUnitId);
            return Ok(result);
        }

        [HttpGet("durations")]
        public async Task<IActionResult> GetDurations([FromQuery] int timeUnitId, [FromQuery] int packageTypeId)
        {
            var result = await _packagePricingService.GetDurationsAndPrices(timeUnitId, packageTypeId);
            return Ok(result);
        }

        [HttpPost("get-pricing")]
        public async Task<IActionResult> GetPricingId([FromBody] PricingLookupDto dto)
        {
            var pricingId = await _packagePricingService.GetPricingIdAsync(dto.TimeUnitId, dto.PackageTypeId, dto.DurationValue);

            if (pricingId == null)
                return NotFound(new { message = "Không tìm thấy gói tương ứng." });

            return Ok(new { pricingId });

        }
        // Post creation and AI generation APIs
        [HttpPost]
        public async Task<IActionResult> CreatePost([FromForm] LandlordPostDto dto)
        {
            // OwnerId should ideally come from authentication in the API, not from a direct user input on the frontend
            // For now, let's assume it's set on the client side or derived from the authenticated user in a real scenario
            // For this example, we'll keep it as is, but a more robust solution would get it from HttpContext.User
            // dto.OwnerId = User.GetUserId(); // This line might need adaptation depending on how authentication is handled in the API
            var postId = await _postService.SavePost(dto);

            return Ok(new
            {
                success = true,
                postId = postId,
                amount = dto.TotalPrice // Ensure TotalPrice is part of LandlordPostDto for this response
            });
        }

        [HttpPost("generate-with-ai")]
        public async Task<IActionResult> GeneratePostWithAI([FromBody] PostDataAIDto model)
        {
            var content = await _azureOpenAIService.GenerateDataPost(model);
            return Ok(new { content });
        }
        // Manage Posts API
        [HttpGet("manage")]
        public async Task<IActionResult> ManagePost([FromQuery] string status = null, [FromQuery] int accountId = 0)
        {
            if (string.IsNullOrEmpty(status))
            {
                status = PostStatusHelper.ToDbValue(PostStatus.Pending);
            }
            if (accountId == 0) return Unauthorized();

            var allPosts = await _postService.GetAllPostsByUserAsync(accountId);

            var statusCounts = allPosts
                .GroupBy(p => p.CurrentStatus)
                .ToDictionary(g => g.Key, g => g.Count());

            var filteredPosts = allPosts
            .Where(p => p.CurrentStatus == status).
            ToList();

            var viewModelList = filteredPosts.Select(f => new ManagePostViewModel // Ensure ManagePostViewModel is in a shared DTO project
            {
                Id = f.PostId,
                Title = f.Title,
                Address = f.Accommodation.Address,
                DistrictName = f.Accommodation.DistrictName ?? "",
                ProvinceName = f.Accommodation.ProvinceName ?? "",
                WardName = f.Accommodation.WardName ?? "",
                Price = f.Accommodation.Price,
                Status = f.CurrentStatus,
                ImageUrl = f.Accommodation?.AccommodationImages?.FirstOrDefault()?.ImageUrl ?? "",
                Area = f.Accommodation?.Area,
                BedroomCount = f.Accommodation?.AccommodationDetail?.BedroomCount,
                BathroomCount = f.Accommodation?.AccommodationDetail?.BathroomCount,
                CreatedAt = f.CreatedAt,
                PackageTypeName = f.PostPackageDetails
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => p.Pricing.PackageType.PackageTypeName)
                    .FirstOrDefault() ?? "",
                RejectionReason = f.PostApprovals
                                .Where(p => p.Status == "R")
                                .OrderByDescending(p => p.ProcessedAt)
                                .Select(p => p.RejectionReason)
                                .FirstOrDefault(),
                AccountName = (f.Account?.UserProfile?.FirstName ?? "") + " " + (f.Account?.UserProfile?.LastName ?? ""),
                AvatarUrl = f.Account?.UserProfile?.AvatarUrl,
                TotalPrice = f.PostPackageDetails?.Select(p => p.Pricing.TotalPrice).FirstOrDefault() ?? 0
            }).ToList();

            // In an API, you return the data directly, not ViewBag.
            return Ok(new { posts = viewModelList, statusCounts = statusCounts });
        }
    }
}