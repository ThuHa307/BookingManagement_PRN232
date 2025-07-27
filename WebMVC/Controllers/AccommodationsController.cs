using BusinessObjects.Dtos;
using Microsoft.AspNetCore.Mvc;
using RentNest.Web.Models;
using System.Text;
using System.Text.Json;
using WebMVC.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using WebMVC.API;


namespace WebMVC.Controllers
{
    public class AccommodationsController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly AccommodationODataApiService _odataService;

        public AccommodationsController(
            HttpClient httpClient, 
            IConfiguration configuration,
            AccommodationODataApiService odataService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _odataService = odataService;
        }

        [HttpGet]
        [Route("danh-sach-phong-tro")]
        public async Task<IActionResult> Index(string? roomType, string? furnitureStatus, int? bedroomCount, int? bathroomCount)
        {
            try
            {
                // Lấy dữ liệu từ TempData
                string provinceName = TempData["provinceName"] as string;
                string districtName = TempData["districtName"] as string;
                string wardName = TempData["wardName"] as string;
                double? area = double.TryParse(TempData["area"] as string, out var a) ? a : null;
                decimal? minMoney = decimal.TryParse(TempData["minMoney"] as string, out var min) ? min : null;
                decimal? maxMoney = decimal.TryParse(TempData["maxMoney"] as string, out var max) ? max : null;

                // Set ViewBag data
                ViewBag.ProvinceName = provinceName;
                ViewBag.DistrictName = districtName;
                ViewBag.WardName = wardName;
                ViewBag.Area = area;
                ViewBag.MinMoney = minMoney;
                ViewBag.MaxMoney = maxMoney;

                // Kiểm tra xem có search results từ TempData không
                var roomListJson = TempData["RoomList"] as string;
                var hasSearched = TempData["HasSearched"] as bool?;

                if (!string.IsNullOrEmpty(roomListJson) && hasSearched == true)
                {
                    // Deserialize search results từ TempData
                    var searchResults = JsonSerializer.Deserialize<List<AccommodationIndexViewModel>>(roomListJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    ViewBag.HasSearched = true;
                    return View(searchResults);
                }

                // Sử dụng OData API thay vì API cũ
                var accommodations = await _odataService.GetAccommodationsAsync();

                // Nếu có filter parameters, tạo OData query
                if (!string.IsNullOrEmpty(roomType) || !string.IsNullOrEmpty(furnitureStatus) || 
                    bedroomCount.HasValue || bathroomCount.HasValue ||
                    !string.IsNullOrEmpty(provinceName) || !string.IsNullOrEmpty(districtName) ||
                    !string.IsNullOrEmpty(wardName) || area.HasValue || minMoney.HasValue || maxMoney.HasValue)
                {
                    var filterQuery = _odataService.BuildODataFilterQuery(
                        roomType: roomType,
                        furnitureStatus: furnitureStatus,
                        bedroomCount: bedroomCount,
                        bathroomCount: bathroomCount,
                        provinceName: provinceName,
                        districtName: districtName,
                        wardName: wardName,
                        area: area,
                        minMoney: minMoney,
                        maxMoney: maxMoney);

                    var completeQuery = _odataService.BuildCompleteODataQuery(filter: filterQuery);
                    accommodations = await _odataService.GetAccommodationsAsync(completeQuery);
                }

                // Lấy danh sách favorite posts
                int accountId = HttpContext.Session.GetInt32("AccountId") ?? 0;
                var favoriteList = await _httpClient.GetFromJsonAsync<List<FavoritePostDto>>(
                    $"{_configuration["ApiSettings:ApiBaseUrl"]}/api/FavoritePost/account/{accountId}");

                var favoritePostIds = favoriteList?.Select(f => f.PostId).ToHashSet() ?? new HashSet<int>();

                // Convert AccommodationDto to AccommodationIndexViewModel
                var model = accommodations.Select(dto => new AccommodationIndexViewModel
                {
                    Id = dto.Id,
                    Status = dto.Status,
                    Title = dto.Title,
                    Price = dto.Price,
                    Address = dto.Address,
                    Area = dto.Area,
                    BathroomCount = dto.BathroomCount,
                    BedroomCount = dto.BedroomCount,
                    ImageUrl = dto.ImageUrl,
                    CreatedAt = dto.CreatedAt,
                    DistrictName = dto.DistrictName,
                    ProvinceName = dto.ProvinceName,
                    WardName = dto.WardName,
                    PackageTypeName = dto.PackageTypeName,
                    TimeUnitName = dto.TimeUnitName,
                    TotalPrice = dto.TotalPrice,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    ListImages = dto.ListImages,
                    PhoneNumber = dto.PhoneNumber,
                    IsFavorite = favoritePostIds.Contains(dto.Id)
                }).ToList();

                ViewBag.HasSearched = !string.IsNullOrEmpty(roomType) || !string.IsNullOrEmpty(furnitureStatus) || 
                                     bedroomCount.HasValue || bathroomCount.HasValue ||
                                     !string.IsNullOrEmpty(provinceName) || !string.IsNullOrEmpty(districtName) ||
                                     !string.IsNullOrEmpty(wardName) || area.HasValue || minMoney.HasValue || maxMoney.HasValue;

                return View(model);
            }
            catch (Exception ex)
            {
                // Log error and return empty list
                ViewBag.HasSearched = false;
                ViewBag.ErrorMessage = "Có lỗi xảy ra khi tải dữ liệu. Vui lòng thử lại sau.";
                Console.WriteLine($"Error fetching accommodations: {ex.Message}");
            }
            return View(new List<AccommodationIndexViewModel>());
        }

        [HttpPost]
        public async Task<IActionResult> Search(string provinceName, string districtName, string? wardName, double? area, decimal? minMoney, decimal? maxMoney, string provinceId, string districtId, string? wardId)
        {
            try
            {
                // Lưu giá trị vào TempData
                TempData["provinceName"] = provinceName;
                TempData["districtName"] = districtName;
                TempData["wardName"] = wardName;
                TempData["provinceId"] = provinceId;
                TempData["districtId"] = districtId;
                TempData["wardId"] = wardId;
                TempData["area"] = area?.ToString();
                TempData["minMoney"] = minMoney?.ToString();
                TempData["maxMoney"] = maxMoney?.ToString();

                if (!ModelState.IsValid)
                {
                    return RedirectToAction("Index");
                }

                // Sử dụng OData API thay vì API cũ
                var accommodations = await _odataService.SearchAccommodationsAsync(
                    provinceName: provinceName,
                    districtName: districtName,
                    wardName: wardName,
                    area: area,
                    minMoney: minMoney,
                    maxMoney: maxMoney,
                    orderBy: "Price desc" // Sắp xếp theo giá giảm dần
                );

                // Convert AccommodationDto to AccommodationIndexViewModel
                var viewModelList = accommodations.Select(dto => new AccommodationIndexViewModel
                {
                    Id = dto.Id,
                    Status = dto.Status,
                    Title = dto.Title,
                    Price = dto.Price,
                    Address = dto.Address,
                    Area = dto.Area,
                    BathroomCount = dto.BathroomCount,
                    BedroomCount = dto.BedroomCount,
                    ImageUrl = dto.ImageUrl,
                    CreatedAt = dto.CreatedAt,
                    DistrictName = dto.DistrictName,
                    ProvinceName = dto.ProvinceName,
                    WardName = dto.WardName,
                    PackageTypeName = dto.PackageTypeName,
                    TimeUnitName = dto.TimeUnitName,
                    TotalPrice = dto.TotalPrice,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    ListImages = dto.ListImages,
                    PhoneNumber = dto.PhoneNumber
                }).ToList();

                // Serialize và lưu vào TempData
                TempData["RoomList"] = JsonSerializer.Serialize(viewModelList, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                TempData["HasSearched"] = true;

                return RedirectToAction("Index", "Accommodations");
            }
            catch (Exception ex)
            {
                // Log error và redirect với thông báo lỗi
                TempData["HasSearched"] = false;
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tìm kiếm. Vui lòng thử lại sau.";
                return RedirectToAction("Index", "Accommodations");
            }
        }

        [HttpGet("chi-tiet/{postId:int}", Name = "PostDetailRoute")]
        public async Task<IActionResult> Detail(int postId)
        {
            var apiUrl = $"{_configuration["ApiSettings:ApiBaseUrl"]}/api/v1/posts/{postId}/detail";
            var response = await _httpClient.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
            {
                return Content("Không tìm thấy dữ liệu chi tiết");
            }

            var json = await response.Content.ReadAsStringAsync();
            var viewModel = JsonSerializer.Deserialize<AccommodationDetailViewModel>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            ViewData["Address"] = viewModel?.Address ?? "";

            return View(viewModel);
        }

        //[Authorize(Roles = "U,L")]
        [HttpGet("bat-dau-tro-chuyen")]
        public async Task<IActionResult> StartConversation(int postId, int receiverId)
        {
            var senderIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(senderIdStr) || !int.TryParse(senderIdStr, out int senderId))
            {
                return Unauthorized();
            }
            // Lấy access token từ session
            var accessToken = HttpContext.Session.GetString("AccessToken");
            if (string.IsNullOrEmpty(accessToken))
            {
                return Unauthorized();
            }
            // Gọi API backend để tạo/lấy hội thoại, gửi kèm token
            var apiUrl = $"{_configuration["ApiSettings:ApiBaseUrl"]}/api/chatroom/start";
            var payload = new { ReceiverId = receiverId, PostId = postId };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            request.Content = content;
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Không thể bắt đầu trò chuyện. Vui lòng thử lại.";
                return RedirectToAction("Detail", new { postId });
            }
            var json = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            int conversationId = doc.RootElement.GetProperty("conversationId").GetInt32();
            TempData["OpenedConversationId"] = conversationId;
            return RedirectToAction("Index", "ChatRoom");
        }
    }
}
