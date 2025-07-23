using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BusinessObjects.Domains;
using BusinessObjects.Dtos.Auth;
using BusinessObjects.Dtos.ChatBot;
using BusinessObjects.Dtos.Post;
using RentNest.Core.DTO;
using RentNest.Core.Model.Momo;
using RentNest.Core.Model.VNPay;
using WebMVC.Models;

namespace WebMVC.API
{
    public class PostApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor; // To get access token if needed

        public PostApiService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _httpClient.BaseAddress = new Uri(_configuration["ApiSettings:ApiBaseUrl"]!);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        // Helper to add authorization header
        private void AddAuthorizationHeader()
        {
            var accessToken = _httpContextAccessor.HttpContext?.Request.Cookies["RentNestAuthToken"]; // Assuming you store token in a cookie
            if (!string.IsNullOrEmpty(accessToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

        private void RemoveAuthorizationHeader()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        public async Task<List<object>> GetPackageTypesByTimeUnit(int timeUnitId)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.GetAsync($"api/v1/posts/package-types/{timeUnitId}");
            RemoveAuthorizationHeader();
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<object>>(); // Adjust return type
        }

        public async Task<List<object>> GetDurations(int timeUnitId, int packageTypeId)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.GetAsync($"api/v1/posts/durations?timeUnitId={timeUnitId}&packageTypeId={packageTypeId}");
            RemoveAuthorizationHeader();
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<object>>(); // Adjust return type
        }

        public async Task<int?> GetPricingId(PricingLookupDto dto)
        {
            AddAuthorizationHeader();
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/v1/posts/get-pricing", content);
            RemoveAuthorizationHeader();

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadFromJsonAsync<JsonElement>();
                return responseContent.GetProperty("pricingId").GetInt32();
            }
            return null;
        }

        public async Task<Tuple<bool, int?, decimal?>> CreatePost(LandlordPostDto dto)
        {
            AddAuthorizationHeader();
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/v1/posts", content);
            RemoveAuthorizationHeader();

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadFromJsonAsync<JsonElement>();
                return Tuple.Create<bool, int?, decimal?>(
                        responseContent.GetProperty("success").GetBoolean(),
                        (int?)responseContent.GetProperty("postId").GetInt32(),
                        (decimal?)responseContent.GetProperty("amount").GetDecimal()
                    );
            }
            return Tuple.Create<bool, int?, decimal?>(false, null, null);
        }

        public async Task<string> GeneratePostWithAI(PostDataAIDto model)
        {
            AddAuthorizationHeader();
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/v1/posts/generate-with-ai", content);
            RemoveAuthorizationHeader();
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadFromJsonAsync<JsonElement>();
            return responseContent.GetProperty("content").GetString();
        }
        public async Task<Tuple<List<ManagePostViewModel>, Dictionary<string, int>>> GetManagePosts(string status, int accountId)
        {
            AddAuthorizationHeader();
            // The API handles filtering by status and accountId
            var response = await _httpClient.GetAsync($"api/v1/posts/manage?status={status}&accountId={accountId}"); // Assuming API supports accountId query
            RemoveAuthorizationHeader();
            response.EnsureSuccessStatusCode();
            var apiResponse = await response.Content.ReadFromJsonAsync<JsonElement>();

            var posts = JsonSerializer.Deserialize<List<ManagePostViewModel>>(apiResponse.GetProperty("posts").GetRawText());
            var statusCounts = JsonSerializer.Deserialize<Dictionary<string, int>>(apiResponse.GetProperty("statusCounts").GetRawText());

            return Tuple.Create(posts, statusCounts);
        }

        public async Task<CreatePostViewModel> GetCreatePostInitialData(int? currentUserId)
        {
            AddAuthorizationHeader();
            var accommodationTypesResponse = await _httpClient.GetAsync("api/v1/accommodation-types");
            var amenitiesResponse = await _httpClient.GetAsync("api/v1/amenities");
            var timePackagesResponse = await _httpClient.GetAsync("api/v1/time-unit-packages");
            var accountResponse = await _httpClient.GetAsync($"api/v1/accounts/{currentUserId}/profile");
            RemoveAuthorizationHeader();

            accommodationTypesResponse.EnsureSuccessStatusCode();
            amenitiesResponse.EnsureSuccessStatusCode();
            timePackagesResponse.EnsureSuccessStatusCode();
            accountResponse.EnsureSuccessStatusCode();

            // **Sửa lỗi tại đây:**
            var accommodationTypes = await accommodationTypesResponse.Content.ReadFromJsonAsync<List<AccommodationType>>();
            var amenities = await amenitiesResponse.Content.ReadFromJsonAsync<List<Amenity>>();
            var timePackages = await timePackagesResponse.Content.ReadFromJsonAsync<List<TimeUnitPackage>>();
            var account = await accountResponse.Content.ReadFromJsonAsync<AccountProfileDto>(); // Sử dụng DTO cụ thể cho Account

            return new CreatePostViewModel
            {
                AccommodationTypes = accommodationTypes,
                Amenities = amenities,
                TimeUnitPackages = timePackages,
                AccountName = $"{account?.FirstName ?? ""} {account?.LastName ?? ""}",
                PhoneNumber = account?.PhoneNumber ?? ""
            };
        }
    }
}