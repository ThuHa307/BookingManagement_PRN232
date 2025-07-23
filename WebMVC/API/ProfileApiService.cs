using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BusinessObjects.Dtos;
using Microsoft.AspNetCore.Http;
using WebMVC.Models;
using WebMVC.Helpers;

namespace WebMVC.API
{
    public class ProfileApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProfileApiService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _httpClient.BaseAddress = new Uri(_configuration["ApiSettings:ApiBaseUrl"]!);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Lấy thông tin profile của user hiện tại
        /// </summary>
        /// <param name="accessToken">JWT Access Token</param>
        /// <returns>ProfileViewModel hoặc null nếu chưa có profile</returns>
        public async Task<ProfileViewModel?> GetMyProfileAsync(string accessToken)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _httpClient.GetAsync("api/UserProfile/my-profile");
                _httpClient.DefaultRequestHeaders.Authorization = null;

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    // Kiểm tra nếu API trả về message "Profile chưa được tạo"
                    if (content.Contains("Profile chưa được tạo") || content.Contains("profile") && content.Contains("null"))
                    {
                        return null; // Profile chưa tồn tại
                    }

                    var userProfileDto = await response.Content.ReadFromJsonAsync<UserProfileDto>();

                    if (userProfileDto == null)
                        return null;

                    // Map DTO sang ViewModel
                    return new ProfileViewModel
                    {
                        FirstName = userProfileDto.FirstName,
                        LastName = userProfileDto.LastName,
                        PhoneNumber = userProfileDto.PhoneNumber,
                        Gender = userProfileDto.Gender,
                        DateOfBirth = userProfileDto.DateOfBirth,
                        AvatarUrl = AvatarHelper.GetFullAvatarUrl(userProfileDto.AvatarUrl, _configuration["ApiSettings:ApiBaseUrl"]!),
                        Occupation = userProfileDto.Occupation,
                        Address = userProfileDto.Address,
                        CreatedAt = userProfileDto.CreatedAt,
                        UpdatedAt = userProfileDto.UpdatedAt
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting profile: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Tạo mới hoặc cập nhật profile
        /// </summary>
        /// <param name="accessToken">JWT Access Token</param>
        /// <param name="profileViewModel">Dữ liệu profile</param>
        /// <returns>True nếu thành công</returns>
        public async Task<bool> CreateOrUpdateProfileAsync(string accessToken, ProfileViewModel profileViewModel)
        {
            try
            {
                // Map ViewModel sang DTO
                var userProfileDto = new UserProfileDto
                {
                    FirstName = profileViewModel.FirstName,
                    LastName = profileViewModel.LastName,
                    PhoneNumber = profileViewModel.PhoneNumber,
                    Gender = profileViewModel.Gender,
                    DateOfBirth = profileViewModel.DateOfBirth,
                    AvatarUrl = profileViewModel.AvatarUrl,
                    Occupation = profileViewModel.Occupation,
                    Address = profileViewModel.Address
                };

                var json = JsonSerializer.Serialize(userProfileDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _httpClient.PostAsync("api/UserProfile/my-profile", content);
                _httpClient.DefaultRequestHeaders.Authorization = null;

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating profile: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Upload avatar
        /// </summary>
        /// <param name="accessToken">JWT Access Token</param>
        /// <param name="avatarFile">File ảnh avatar</param>
        /// <returns>URL của avatar nếu thành công</returns>
        public async Task<string?> UploadAvatarAsync(string accessToken, IFormFile avatarFile)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                using var fileContent = new StreamContent(avatarFile.OpenReadStream());
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(avatarFile.ContentType);
                content.Add(fileContent, "avatar", avatarFile.FileName);

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _httpClient.PostAsync("api/UserProfile/avatar", content);
                _httpClient.DefaultRequestHeaders.Authorization = null;

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseObj = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (responseObj.TryGetProperty("avatarUrl", out var avatarUrlElement))
                    {
                        return avatarUrlElement.GetString();
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading avatar: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Xóa avatar
        /// </summary>
        /// <param name="accessToken">JWT Access Token</param>
        /// <returns>True nếu thành công</returns>
        public async Task<bool> DeleteAvatarAsync(string accessToken)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _httpClient.DeleteAsync("api/UserProfile/avatar");
                _httpClient.DefaultRequestHeaders.Authorization = null;

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting avatar: {ex.Message}");
                return false;
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
                    var loginResponse = await response.Content.ReadFromJsonAsync<BusinessObjects.Dtos.Auth.LoginResponseDto>();
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