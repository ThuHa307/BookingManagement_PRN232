using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BusinessObjects.Dtos.Auth;
using WebMVC.Models;

namespace WebMVC.API
{
    public class PasswordApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PasswordApiService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _httpClient.BaseAddress = new Uri(_configuration["ApiSettings:ApiBaseUrl"]!);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        /// <param name="accessToken">JWT Access Token</param>
        /// <param name="model">Thông tin đổi mật khẩu</param>
        /// <returns>Kết quả thành công/thất bại và message</returns>
        public async Task<(bool success, string message)> ChangePasswordAsync(string accessToken, ChangePasswordViewModel model)
        {
            try
            {
                // Map ViewModel sang DTO
                var changePasswordDto = new ChangePasswordDto
                {
                    CurrentPassword = model.CurrentPassword,
                    NewPassword = model.NewPassword,
                    ConfirmPassword = model.ConfirmPassword
                };

                var json = JsonSerializer.Serialize(changePasswordDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _httpClient.PostAsync("api/Auth/change-password", content);
                _httpClient.DefaultRequestHeaders.Authorization = null;

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ChangePasswordResponseDto>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return (result?.Success ?? false, result?.Message ?? "Đổi mật khẩu thành công");
                }
                else
                {
                    // Parse error response
                    try
                    {
                        var errorResult = JsonSerializer.Deserialize<ChangePasswordResponseDto>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        return (false, errorResult?.Message ?? "Đổi mật khẩu thất bại");
                    }
                    catch
                    {
                        // Fallback error message
                        return response.StatusCode switch
                        {
                            System.Net.HttpStatusCode.Unauthorized => (false, "Phiên đăng nhập đã hết hạn"),
                            System.Net.HttpStatusCode.BadRequest => (false, "Thông tin không hợp lệ"),
                            System.Net.HttpStatusCode.NotFound => (false, "Không tìm thấy tài khoản"),
                            _ => (false, "Có lỗi xảy ra khi đổi mật khẩu")
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error changing password: {ex.Message}");
                return (false, "Có lỗi xảy ra khi kết nối đến server");
            }
        }

        /// <summary>
        /// Kiểm tra và refresh token nếu cần
        /// </summary>
        /// <returns>Access token mới hoặc null nếu thất bại</returns>
        public async Task<string?> RefreshTokenIfNeededAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/Auth/refresh-token");

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
                    return loginResponse?.AccessToken;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing token: {ex.Message}");
                return null;
            }
        }
    }
}