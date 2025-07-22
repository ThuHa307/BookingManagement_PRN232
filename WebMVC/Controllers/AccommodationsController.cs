using Microsoft.AspNetCore.Mvc;
using RentNest.Web.Models;
using WebMVC.Models;
using System.Text.Json;
using System.Text;

namespace WebMVC.Controllers
{
    public class AccommodationsController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public AccommodationsController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
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

                // Build query string for API call
                var queryParams = new List<string>();

                if (!string.IsNullOrEmpty(roomType))
                    queryParams.Add($"roomType={Uri.EscapeDataString(roomType)}");
                if (!string.IsNullOrEmpty(furnitureStatus))
                    queryParams.Add($"furnitureStatus={Uri.EscapeDataString(furnitureStatus)}");
                if (bedroomCount.HasValue)
                    queryParams.Add($"bedroomCount={bedroomCount.Value}");
                if (bathroomCount.HasValue)
                    queryParams.Add($"bathroomCount={bathroomCount.Value}");
                if (!string.IsNullOrEmpty(provinceName))
                    queryParams.Add($"provinceName={Uri.EscapeDataString(provinceName)}");
                if (!string.IsNullOrEmpty(districtName))
                    queryParams.Add($"districtName={Uri.EscapeDataString(districtName)}");
                if (!string.IsNullOrEmpty(wardName))
                    queryParams.Add($"wardName={Uri.EscapeDataString(wardName)}");
                if (area.HasValue)
                    queryParams.Add($"area={area.Value}");
                if (minMoney.HasValue)
                    queryParams.Add($"minMoney={minMoney.Value}");
                if (maxMoney.HasValue)
                    queryParams.Add($"maxMoney={maxMoney.Value}");

                var queryString = string.Join("&", queryParams);
                string apiUrl = $"{_configuration["ApiSettings:ApiBaseUrl"]}/api/accommodations";

                // Call WebAPI
                var response = await _httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var apiResponse = JsonSerializer.Deserialize<AccommodationApiResponse>(jsonContent, options);

                    // Convert AccommodationDto to AccommodationIndexViewModel
                    var model = apiResponse.Data.Select(dto => new AccommodationIndexViewModel
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

                    ViewBag.HasSearched = apiResponse.HasSearched;
                    return View(model);
                }
                else
                {
                    // Fallback to empty list if API fails
                    ViewBag.HasSearched = false;
                    return View(new List<AccommodationIndexViewModel>());
                }
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

                // Tạo request object
                var searchRequest = new SearchViewModel
                {
                    ProvinceName = provinceName,
                    DistrictName = districtName,
                    WardName = wardName,
                    Area = area,
                    MinMoney = minMoney,
                    MaxMoney = maxMoney,
                    ProvinceId = provinceId,
                    DistrictId = districtId,
                    WardId = wardId
                };

                // Serialize request to JSON
                var jsonContent = JsonSerializer.Serialize(searchRequest, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Call WebAPI
                var apiUrl = $"{_configuration["ApiBaseUrl"]}/api/accommodations/search";
                var response = await _httpClient.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var apiResponse = JsonSerializer.Deserialize<SearchApiResponse>(responseContent, options);

                    // Convert AccommodationDto to AccommodationIndexViewModel
                    var viewModelList = apiResponse.Data.Select(dto => new AccommodationIndexViewModel
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
                else
                {
                    // Nếu API call thất bại, set HasSearched = false và redirect
                    TempData["HasSearched"] = false;
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi tìm kiếm. Vui lòng thử lại sau.";
                    return RedirectToAction("Index", "Accommodations");
                }
            }
            catch (Exception ex)
            {
                // Log error và redirect với thông báo lỗi
                TempData["HasSearched"] = false;
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tìm kiếm. Vui lòng thử lại sau.";
                return RedirectToAction("Index", "Accommodations");
            }
        }
    }
}
