using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BusinessObjects.Dtos.Auth;
using Microsoft.AspNetCore.Authentication;
using WebMVC.Models;

namespace WebMVC.API
{
    public class AuthApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public AuthApiService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _httpClient.BaseAddress = new Uri(_configuration["ApiSettings:ApiBaseUrl"]!);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        // --- AUTHENTICATION ---

        // CHỈNH SỬA PHƯƠNG THỨC REGISTERASYNC NÀY
        public async Task<string?> RegisterAsync(AccountRegisterDto model) // <- Đảm bảo kiểu dữ liệu model là AccountRegisterDto
        {
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/Auth/register", content); // Đảm bảo đúng endpoint API

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync(); // API trả về một thông báo thành công
            }
            else
            {
                // Đọc nội dung lỗi từ API để gỡ lỗi chi tiết hơn nếu cần
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Register Error: {errorContent}"); // In ra console để debug
                // Bạn có thể parse errorContent thành một DTO lỗi nếu API trả về cấu trúc lỗi cụ thể
                // Hoặc đơn giản trả về null để báo hiệu lỗi
                return null;
            }
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto model)
        {
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/Auth/login", content);

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
                return loginResponse;
            }
            else
            {
                // Xử lý lỗi, ví dụ: Invalid credentials
                return null;
            }
        }

        public async Task<LoginResponseDto?> LoginTwoFactorAsync(TwoFactorLoginDto model)
        {
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/Auth/login-2fa", content);
            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
                // Xóa session ID sau khi 2FA hoàn tất
                _httpContextAccessor.HttpContext?.Session.Remove("TwoFactorSessionId");
                return loginResponse;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                // Đặt breakpoint ở đây và kiểm tra giá trị của 'errorContent'
                // Nó sẽ cho bạn biết chính xác lỗi từ backend (nếu có)
                Console.Write($"API call to login-2fa failed: {response.StatusCode} - {errorContent}");
                return null;
            }
        }

        public async Task<LoginResponseDto?> LoginWithRecoveryCodeAsync(TwoFactorRecoveryCodeLoginViewModel model)
        {
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var sessionId = _httpContextAccessor.HttpContext?.Session.GetString("TwoFactorSessionId");
            if (!string.IsNullOrEmpty(sessionId))
            {
                _httpClient.DefaultRequestHeaders.Add("X-TwoFactor-Session", sessionId);
            }

            var response = await _httpClient.PostAsync("api/Auth/login-recovery-code", content);

            if (!string.IsNullOrEmpty(sessionId))
            {
                _httpClient.DefaultRequestHeaders.Remove("X-TwoFactor-Session");
            }

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
                _httpContextAccessor.HttpContext?.Session.Remove("TwoFactorSessionId");
                return loginResponse;
            }
            else
            {
                return null;
            }
        }

        public async Task<TwoFactorStatusDto?> GetTwoFactorStatusAsync(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _httpClient.GetAsync("api/Auth/2fa-status");
            _httpClient.DefaultRequestHeaders.Authorization = null;

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TwoFactorStatusDto>();
            }
            return null;
        }

        public async Task LogoutAsync()
        {
            // Xóa token từ cookie của client trước khi gọi API Logout
            await _httpContextAccessor.HttpContext!.SignOutAsync("Cookies");

            // Gọi API logout để xóa refreshToken trên server
            await _httpClient.PostAsync("api/Auth/logout", null);
        }
    }
}