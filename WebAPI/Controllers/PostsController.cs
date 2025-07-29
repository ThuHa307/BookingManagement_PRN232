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

        [HttpGet("{postId:int}/detail")]
        public async Task<IActionResult> GetPostDetail(int postId)
        {
            var post = await _postService.GetPostDetailWithAccommodationDetailAsync(postId);

            if (post == null || post.Accommodation == null || post.Accommodation.AccommodationDetail == null)
            {
                return NotFound("Không tìm thấy dữ liệu chi tiết");
            }

            var imageUrls = post.Accommodation.AccommodationImages?
                .Select(img => img.ImageUrl)
                .ToList() ?? new List<string>();

            var latestPackage = post.PostPackageDetails
                 .OrderByDescending(p => p.StartDate)
                 .FirstOrDefault();

            var viewModel = new AccommodationDetailViewModel
            {
                PostId = post.PostId,
                PostTitle = post.Title,
                PostContent = post.Content,
                DetailId = post.Accommodation.AccommodationDetail.DetailId,
                AccommodationId = post.Accommodation.AccommodationId,
                ImageUrls = imageUrls,
                Price = post.Accommodation.Price,
                Description = post.Accommodation.Description,
                BathroomCount = post.Accommodation.AccommodationDetail.BathroomCount,
                BedroomCount = post.Accommodation.AccommodationDetail.BedroomCount,
                HasKitchenCabinet = post.Accommodation.AccommodationDetail.HasKitchenCabinet,
                HasAirConditioner = post.Accommodation.AccommodationDetail.HasAirConditioner,
                HasRefrigerator = post.Accommodation.AccommodationDetail.HasRefrigerator,
                HasWashingMachine = post.Accommodation.AccommodationDetail.HasWashingMachine,
                HasLoft = post.Accommodation.AccommodationDetail.HasLoft,
                FurnitureStatus = post.Accommodation.AccommodationDetail.FurnitureStatus,
                CreatedAt = post.Accommodation.AccommodationDetail.CreatedAt,
                UpdatedAt = post.Accommodation.AccommodationDetail.UpdatedAt,
                Address = post.Accommodation.Address ?? "",
                AccountId = post.Account.AccountId,
                AccountImg = post.Account.UserProfile.AvatarUrl ?? "/images/default-avatar.jpg",
                AccountName = post.Account.UserProfile.FirstName + " " + post.Account.UserProfile.LastName,
                AccountPhone = post.Account.UserProfile.PhoneNumber,
                Amenities = post.Accommodation.AccommodationAmenities?
                    .Where(a => a.Amenity != null)
                    .Select(a => a.Amenity.AmenityName)
                    .ToList() ?? new List<string>(),
                DistrictName = post.Accommodation.DistrictName ?? "",
                ProvinceName = post.Accommodation.ProvinceName ?? "",
                WardName = post.Accommodation.WardName ?? "",

                PackageTypeName = latestPackage?.Pricing?.PackageType?.PackageTypeName ?? "Tin thường",
                TimeUnitName = latestPackage?.Pricing?.TimeUnit?.TimeUnitName ?? "",
                TotalPrice = latestPackage?.TotalPrice ?? 0,
                StartDate = latestPackage?.StartDate,
                EndDate = latestPackage?.EndDate,
                CurrentStatus = post.CurrentStatus
            };

            return Ok(viewModel);
        }
        [HttpPost("score")]
        public async Task<IActionResult> ScoreComment([FromBody] CommentScoreDto model)
        {
            if (string.IsNullOrWhiteSpace(model.PostContent) || string.IsNullOrWhiteSpace(model.CommentContent))
            {
                return BadRequest("PostContent và CommentContent không được để trống.");
            }

            try
            {
                var score = await _azureOpenAIService.ScoreCommentAsync(model);
                return Ok(new { Score = score });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Đã xảy ra lỗi khi chấm điểm.", Error = ex.Message });
            }
        }
        [HttpPost("score/average")]
        public async Task<IActionResult> ScoreAverage([FromBody] PostWithCommentsDto model)
        {
            if (string.IsNullOrWhiteSpace(model.PostContent) || model.CommentContents == null || !model.CommentContents.Any())
            {
                return BadRequest("Bài viết và danh sách bình luận không được để trống.");
            }

            try
            {
                var avgScore = await _azureOpenAIService.ScoreAverageCommentAsync(model);
                return Ok(new { AverageScore = avgScore });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Đã xảy ra lỗi khi tính điểm.", Error = ex.Message });
            }
        }

        [HttpGet("{postId:int}/comments")]
        public async Task<IActionResult> GetCommentsForPost(int postId)
        {
            var comments = await _postService.GetCommentsByPostId(postId);
            var result = comments.Select(c => new { c.CommentId, c.Content, c.CreatedAt }).ToList();
            return Ok(result);
        }

        [HttpGet("admin")]
        public async Task<IActionResult> GetAllPostsForAdmin()
        {
            var posts = await _postService.GetAllPostsWithAccommodation();

            var result = posts.Select(p => new
            {
                PostId = p.PostId,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                Status = p.CurrentStatus,
                CreatorName = (p.Account?.UserProfile?.FirstName ?? "") + " " + (p.Account?.UserProfile?.LastName ?? "")
            }).ToList();

            return Ok(result);
        }

        // Thêm vào PostsController.cs (API)

        [HttpGet("all-with-accommodation")]
        public async Task<IActionResult> GetAllPostsWithAccommodation()
        {
            try
            {
                var posts = await _postService.GetAllPosts();

                var result = posts.Select(p => new
                {
                    PostId = p.PostId,
                    Title = p.Title ?? "Không có tiêu đề",
                    Content = p.Content ?? "Không có nội dung",
                    CreatedAt = p.CreatedAt,
                    CurrentStatus = p.CurrentStatus,
                    Account = new
                    {
                        AccountId = p.Account?.AccountId ?? 0,
                        UserProfile = new
                        {
                            FirstName = p.Account?.UserProfile?.FirstName ?? "",
                            LastName = p.Account?.UserProfile?.LastName ?? "",
                            AvatarUrl = p.Account?.UserProfile?.AvatarUrl ?? ""
                        }
                    },
                    Accommodation = p.Accommodation != null ? new
                    {
                        AccommodationId = p.Accommodation.AccommodationId,
                        Address = p.Accommodation.Address ?? "",
                        Price = p.Accommodation.Price,
                        Area = p.Accommodation.Area
                    } : null
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Đã xảy ra lỗi khi lấy danh sách bài viết.", Error = ex.Message });
            }
        }



    }
}