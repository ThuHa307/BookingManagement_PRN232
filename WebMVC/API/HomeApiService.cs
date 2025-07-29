using System.Text.Json;
using System.Text;
using WebMVC.Models;

namespace WebMVC.API
{
    public class HomeApiService : IHomeApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HomeApiService> _logger;
        private readonly IConfiguration _configuration;

        public HomeApiService(HttpClient httpClient, ILogger<HomeApiService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;

            // Set base address for API
            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5290";
            _httpClient.BaseAddress = new Uri(apiBaseUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<ApiResponse<string>> SendContactAsync(ContactFormViewModel model)
        {
            try
            {
                var json = JsonSerializer.Serialize(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/HomeApi/contact", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<string>>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return apiResponse ?? new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Không thể xử lý phản hồi từ server"
                    };
                }

                // Try to parse error response
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<ApiResponse<string>>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return errorResponse ?? new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Có lỗi xảy ra khi gửi email"
                    };
                }
                catch
                {
                    return new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Có lỗi xảy ra khi gửi email"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling SendContact API");
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Không thể kết nối đến server"
                };
            }
        }

        public async Task<ContactInfo> GetContactInfoAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/HomeApi/contact-info");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<ContactInfo>>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return apiResponse?.Data ?? GetDefaultContactInfo();
                }

                _logger.LogWarning($"API call failed with status: {response.StatusCode}");
                return GetDefaultContactInfo();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling GetContactInfo API");
                return GetDefaultContactInfo();
            }
        }

        private ContactInfo GetDefaultContactInfo()
        {
            return new ContactInfo
            {
                Address = "Đại Học FPT, Hòa Hải, Ngũ Hành Sơn, Da Nang 550000, Vietnam",
                Email = "bluedream.rentnest.company@gmail.com",
                Phone = "(+84) 941 673 660",
                Website = "blueHouseDaNang.com"
            };
        }
    }
    }
