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
using System.Text.Json.Serialization; // Thêm namespace này

namespace WebMVC.API
{
    public class PostApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor; // To get access token if needed
        private readonly JsonSerializerOptions _jsonSerializerOptions;


        public PostApiService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _httpClient.BaseAddress = new Uri(_configuration["ApiSettings:ApiBaseUrl"]!);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true, // Quan trọng nhất!
                ReferenceHandler = ReferenceHandler.IgnoreCycles, // Giữ nguyên nếu cần
                // Nếu bạn muốn tên thuộc tính được chuyển đổi từ PascalCase sang camelCase khi serialize (ít liên quan ở đây nhưng tốt nếu có)
                // PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            };
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

            using var form = new MultipartFormDataContent();

            // Add scalar values
            form.Add(new StringContent(dto.Address), "address");
            form.Add(new StringContent(dto.AccommodationTypeId.ToString()), "accommodationTypeId");
            form.Add(new StringContent(dto.Area.ToString()), "area");
            form.Add(new StringContent(dto.Price.ToString()), "Price");

            if (!string.IsNullOrEmpty(dto.FurnitureStatus))
                form.Add(new StringContent(dto.FurnitureStatus), "furnitureStatus");

            if (dto.NumberBedroom.HasValue)
                form.Add(new StringContent(dto.NumberBedroom.ToString()), "numberBedroom");

            if (dto.NumberBathroom.HasValue)
                form.Add(new StringContent(dto.NumberBathroom.ToString()), "numberBathroom");

            form.Add(new StringContent(dto.HasKitchenCabinet?.ToString() ?? "false"), "hasKitchenCabinet");
            form.Add(new StringContent(dto.HasAirConditioner?.ToString() ?? "false"), "hasAirConditioner");
            form.Add(new StringContent(dto.HasRefrigerator?.ToString() ?? "false"), "hasRefrigerator");
            form.Add(new StringContent(dto.HasWashingMachine?.ToString() ?? "false"), "hasWashingMachine");
            form.Add(new StringContent(dto.HasLoft?.ToString() ?? "false"), "hasLoft");

            form.Add(new StringContent(dto.titlePost), "titlePost");
            form.Add(new StringContent(dto.contentPost), "contentPost");

            if (dto.AmenityIds != null)
            {
                foreach (var id in dto.AmenityIds)
                {
                    form.Add(new StringContent(id.ToString()), "AmenityIds");
                }
            }

            if (!string.IsNullOrEmpty(dto.AccommodationDescription))
                form.Add(new StringContent(dto.AccommodationDescription), "accommodationDescription");

            if (dto.PricingId.HasValue)
                form.Add(new StringContent(dto.PricingId.ToString()), "pricingId");

            if (dto.OwnerId.HasValue)
                form.Add(new StringContent(dto.OwnerId.ToString()), "ownerId");

            form.Add(new StringContent(dto.TotalPrice.ToString()), "totalPrice");
            form.Add(new StringContent(dto.StartDate.ToString("yyyy-MM-dd")), "startDate");
            form.Add(new StringContent(dto.EndDate.ToString("yyyy-MM-dd")), "endDate");

            // Add files
            if (dto.Images != null)
            {
                foreach (var file in dto.Images)
                {
                    var streamContent = new StreamContent(file.OpenReadStream());
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                    form.Add(streamContent, "images", file.FileName); // lowercase "images" to match DTO
                }
            }

            var response = await _httpClient.PostAsync("api/v1/posts", form);
            RemoveAuthorizationHeader();

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadFromJsonAsync<JsonElement>();
                return Tuple.Create(
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
            string requestUri = $"api/v1/posts/manage?status={status}&accountId={accountId}";

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.GetAsync(requestUri);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException httpEx)
            {
                // Bạn có thể log lỗi ở đây nếu cần cho production, nhưng đã bỏ _logger.Log...
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                RemoveAuthorizationHeader();
            }

            JsonElement apiResponse;
            try
            {
                var responseContent = await response.Content.ReadAsStringAsync();

                // Deserialize thẳng JSONElement với options đã cấu hình
                apiResponse = JsonSerializer.Deserialize<JsonElement>(responseContent, _jsonSerializerOptions);

                if (!apiResponse.TryGetProperty("posts", out var postsElement) || !apiResponse.TryGetProperty("statusCounts", out var statusCountsElement))
                {
                    return Tuple.Create(new List<ManagePostViewModel>(), new Dictionary<string, int>());
                }

                // Truyền options vào đây!
                var posts = JsonSerializer.Deserialize<List<ManagePostViewModel>>(postsElement.GetRawText(), _jsonSerializerOptions);
                // Truyền options vào đây!
                var statusCounts = JsonSerializer.Deserialize<Dictionary<string, int>>(statusCountsElement.GetRawText(), _jsonSerializerOptions);

                return Tuple.Create(posts, statusCounts);
            }
            catch (JsonException jsonEx)
            {
                return Tuple.Create(new List<ManagePostViewModel>(), new Dictionary<string, int>());
            }
            catch (Exception ex)
            {
                throw;
            }
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

        public async Task<double?> ScoreAverageCommentAsync(string postContent, List<string> commentContents)
        {
            var dto = new PostWithCommentsDto
            {
                PostContent = postContent,
                CommentContents = commentContents
            };

            var response = await _httpClient.PostAsJsonAsync("/api/v1/posts/score/average", dto);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<Dictionary<string, double>>();
                return data != null && data.ContainsKey("averageScore") ? data["averageScore"] : (double?)null;
            }

            return null;
        }

        public async Task<List<Comment>> GetCommentsForPost(int postId)
        {
            var response = await _httpClient.GetAsync($"/api/v1/posts/{postId}/comments");
            return await response.Content.ReadFromJsonAsync<List<Comment>>() ?? new List<Comment>();
        }

        public async Task<AccommodationDetailViewModel?> GetPostDetail(int postId)
        {
            var response = await _httpClient.GetAsync($"/api/v1/posts/{postId}/detail");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AccommodationDetailViewModel>();
            }

            return null;
        }

        // Cập nhật method GetAllPostsWithAccommodation trong PostApiService:

        public async Task<List<AdminPostDto>> GetAllPostsWithAccommodation()
        {
            AddAuthorizationHeader();
            try
            {
                // Sử dụng endpoint admin có sẵn
                var response = await _httpClient.GetAsync("api/v1/posts/all-with-accommodation");
                RemoveAuthorizationHeader();

                if (!response.IsSuccessStatusCode)
                {
                    return new List<AdminPostDto>();
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var posts = JsonSerializer.Deserialize<List<JsonElement>>(jsonResponse, _jsonSerializerOptions);

                var result = new List<AdminPostDto>();

                foreach (var post in posts)
                {
                    var adminPost = new AdminPostDto
                    {
                        PostId = post.TryGetProperty("postId", out var postId) ? postId.GetInt32() : 0,
                        Title = post.TryGetProperty("title", out var content) ? content.GetString() ?? "Không có tiêu đề" : "Không có tiêu đề",
                        Content = post.TryGetProperty("content", out var contentProp) ? contentProp.GetString() ?? "Không có nội dung" : "Không có nội dung",
                        CreatorName = post.TryGetProperty("creatorName", out var creator) ? creator.GetString() ?? "Không xác định" : "Không xác định",
                        Status = post.TryGetProperty("currentStatus", out var status) ? status.GetString() ?? "P" : "P",
                        CreatedAt = post.TryGetProperty("createdAt", out var createdAt) ?
                            DateTime.TryParse(createdAt.GetString(), out var date) ? date : DateTime.Now
                            : DateTime.Now
                    };

                    result.Add(adminPost);
                }

                return result;
            }
            catch (Exception)
            {
                RemoveAuthorizationHeader();
                return new List<AdminPostDto>();
            }
        }

        // Thêm DTO mới để mapping dữ liệu
        public class AdminPostDto
        {
            public int PostId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public string CreatorName { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }

    }
}