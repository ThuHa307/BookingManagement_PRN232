using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BusinessObjects.Dtos.Auth;
using WebMVC.Models;
using QRCoder;
namespace WebMVC.API
{
    public class AccountApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public AccountApiService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _httpClient.BaseAddress = new Uri(_configuration["ApiSettings:ApiBaseUrl"]!);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        public async Task<UserInfoResponseDto?> GetUserInfoAsync(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _httpClient.GetAsync("api/Auth/userInfo");
            _httpClient.DefaultRequestHeaders.Authorization = null; // Xóa header sau khi dùng

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserInfoResponseDto>();
            }
            return null;
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

        public async Task<TwoFactorSetupInfoResponseDto?> GetTwoFactorSetupInfoAsync(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _httpClient.GetAsync("api/Auth/2fa-setup-info");
            _httpClient.DefaultRequestHeaders.Authorization = null;

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TwoFactorSetupInfoResponseDto>();
            }
            return null;
        }

        public async Task<TwoFactorEnableResponseDto?> EnableTwoFactorAsync(string accessToken, TwoFactorVerificationViewModel model)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/Auth/2fa-enable", content);
            _httpClient.DefaultRequestHeaders.Authorization = null;

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TwoFactorEnableResponseDto>();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                // Optionally throw an exception or return a specific error DTO
                return null;
            }
        }

        public async Task<bool> DisableTwoFactorAsync(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _httpClient.PostAsync("api/Auth/2fa-disable", null);
            _httpClient.DefaultRequestHeaders.Authorization = null;

            return response.IsSuccessStatusCode;
        }

        // Helper để tạo QR code (có thể đặt ở một service riêng nếu muốn)
        public string GenerateQrCodeUri(string authenticatorUri)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new PngByteQRCode(qrCodeData))
                {
                    byte[] qrCodeBytes = qrCode.GetGraphic(20); // Scale 20, để mã QR to và rõ ràng
                    return "data:image/png;base64," + Convert.ToBase64String(qrCodeBytes);
                }
            }
        }
    }
}