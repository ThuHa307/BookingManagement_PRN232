using BusinessObjects.Dtos;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccommodationsController : ControllerBase
    {
        private readonly IAccommodationService _accommodationService;
        private readonly IPostService _postService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccommodationsController(
            IAccommodationService accommodationService, 
            IPostService postService,
            IHttpContextAccessor httpContextAccessor)
        {
            _accommodationService = accommodationService;
            _postService = postService;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        public async Task<IActionResult> GetAccommodations(
            string? roomType, 
            string? furnitureStatus, 
            int? bedroomCount, 
            int? bathroomCount,
            string? provinceName,
            string? districtName,
            string? wardName,
            double? area,
            decimal? minMoney,
            decimal? maxMoney)
        {
            try
            {
                var posts = await _postService.GetAllPostsWithAccommodation();

                var accommodations = posts.Select(p =>
                {
                    var latestPackageDetail = p.PostPackageDetails
                        .OrderByDescending(d => d.StartDate)
                        .FirstOrDefault();

                    return new AccommodationDto
                    {
                        Id = p.PostId,
                        Status = p.CurrentStatus,
                        Title = p.Title,
                        Price = p.Accommodation.Price,
                        Address = p.Accommodation.Address,
                        Area = p.Accommodation.Area,
                        BathroomCount = p.Accommodation?.AccommodationDetail?.BathroomCount,
                        BedroomCount = p.Accommodation?.AccommodationDetail?.BedroomCount,
                        ImageUrl = p.Accommodation?.AccommodationImages?.FirstOrDefault()?.ImageUrl ?? "default-image.jpg",
                        CreatedAt = p.CreatedAt,
                        DistrictName = p.Accommodation.DistrictName ?? "",
                        ProvinceName = p.Accommodation.ProvinceName ?? "",
                        WardName = p.Accommodation.WardName ?? "",
                        PackageTypeName = latestPackageDetail?.Pricing?.PackageType?.PackageTypeName ?? "",
                        TimeUnitName = latestPackageDetail?.Pricing?.TimeUnit?.TimeUnitName ?? "",
                        TotalPrice = latestPackageDetail?.TotalPrice ?? 0,
                        StartDate = latestPackageDetail?.StartDate,
                        EndDate = latestPackageDetail?.EndDate,
                        ListImages = p.Accommodation?.AccommodationImages?.Select(i => i.ImageUrl).ToList() ?? new List<string>(),
                        PhoneNumber = p.Account.UserProfile?.PhoneNumber ?? ""
                    };
                }).ToList();

                // Apply filters
                if (!string.IsNullOrEmpty(roomType))
                {
                    accommodations = accommodations.Where(m => m.Title.Contains(roomType, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrEmpty(furnitureStatus))
                {
                    accommodations = accommodations.Where(m => m.Status == furnitureStatus).ToList();
                }

                if (bedroomCount.HasValue)
                {
                    accommodations = accommodations.Where(m => m.BedroomCount == bedroomCount).ToList();
                }

                if (bathroomCount.HasValue)
                {
                    accommodations = accommodations.Where(m => m.BathroomCount == bathroomCount).ToList();
                }

                // Apply search filters from TempData equivalent
                if (!string.IsNullOrEmpty(provinceName))
                {
                    accommodations = accommodations.Where(m => m.ProvinceName.Contains(provinceName, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrEmpty(districtName))
                {
                    accommodations = accommodations.Where(m => m.DistrictName.Contains(districtName, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrEmpty(wardName))
                {
                    accommodations = accommodations.Where(m => m.WardName.Contains(wardName, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (area.HasValue)
                {
                    accommodations = accommodations.Where(m => m.Area >= area.Value).ToList();
                }

                if (minMoney.HasValue)
                {
                    accommodations = accommodations.Where(m => m.Price >= minMoney.Value).ToList();
                }

                if (maxMoney.HasValue)
                {
                    accommodations = accommodations.Where(m => m.Price <= maxMoney.Value).ToList();
                }

                return Ok(new
                {
                    Data = accommodations,
                    HasSearched = !string.IsNullOrEmpty(roomType) || !string.IsNullOrEmpty(furnitureStatus) || 
                                  bedroomCount.HasValue || bathroomCount.HasValue ||
                                  !string.IsNullOrEmpty(provinceName) || !string.IsNullOrEmpty(districtName) ||
                                  !string.IsNullOrEmpty(wardName) || area.HasValue || minMoney.HasValue || maxMoney.HasValue,
                    Filters = new
                    {
                        ProvinceName = provinceName,
                        DistrictName = districtName,
                        WardName = wardName,
                        Area = area,
                        MinMoney = minMoney,
                        MaxMoney = maxMoney,
                        RoomType = roomType,
                        FurnitureStatus = furnitureStatus,
                        BedroomCount = bedroomCount,
                        BathroomCount = bathroomCount
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] SearchRequestDto searchRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var accommodations = await _accommodationService.GetAccommodationsBySearchDto(
                    searchRequest.ProvinceName, 
                    searchRequest.DistrictName, 
                    searchRequest.WardName, 
                    searchRequest.Area, 
                    searchRequest.MinMoney, 
                    searchRequest.MaxMoney);

                var viewModelList = accommodations.Select(p =>
                {
                    var latestPackageDetail = p.PostPackageDetails
                        ?.OrderByDescending(d => d.StartDate)
                        .FirstOrDefault();

                    return new AccommodationDto
                    {
                        Id = p.PostId,
                        Status = p.CurrentStatus ?? "",
                        Title = p.Title ?? "",
                        Price = p.Accommodation?.Price ?? 0,
                        Address = p.Accommodation?.Address ?? "",
                        Area = p.Accommodation?.Area ?? 0,
                        BathroomCount = p.Accommodation?.AccommodationDetail?.BathroomCount,
                        BedroomCount = p.Accommodation?.AccommodationDetail?.BedroomCount,
                        ImageUrl = p.Accommodation?.AccommodationImages?.FirstOrDefault()?.ImageUrl ?? "default-image.jpg",
                        CreatedAt = p.CreatedAt,
                        DistrictName = p.Accommodation?.DistrictName ?? "",
                        ProvinceName = p.Accommodation?.ProvinceName ?? "",
                        WardName = p.Accommodation?.WardName ?? "",
                        PackageTypeName = latestPackageDetail?.Pricing?.PackageType?.PackageTypeName ?? "",
                        TimeUnitName = latestPackageDetail?.Pricing?.TimeUnit?.TimeUnitName ?? "",
                        TotalPrice = latestPackageDetail?.TotalPrice ?? 0,
                        StartDate = latestPackageDetail?.StartDate,
                        EndDate = latestPackageDetail?.EndDate,
                        ListImages = p.Accommodation?.AccommodationImages?.Select(i => i.ImageUrl).ToList() ?? new List<string>(),
                        PhoneNumber = p.Account?.UserProfile?.PhoneNumber ?? ""
                    };
                }).ToList();

                return Ok(new
                {
                    Data = viewModelList,
                    HasSearched = true,
                    SearchParams = new
                    {
                        ProvinceName = searchRequest.ProvinceName,
                        DistrictName = searchRequest.DistrictName,
                        WardName = searchRequest.WardName,
                        ProvinceId = searchRequest.ProvinceId,
                        DistrictId = searchRequest.DistrictId,
                        WardId = searchRequest.WardId,
                        Area = searchRequest.Area,
                        MinMoney = searchRequest.MinMoney,
                        MaxMoney = searchRequest.MaxMoney
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
} 