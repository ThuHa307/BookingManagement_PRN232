using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BusinessObjects.Dtos.Account;
using BusinessObjects.Dtos.Auth;
using BusinessObjects.Dtos.Common;
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

        // Authentication and 2FA Methods
        public async Task<UserInfoResponseDto?> GetUserInfoAsync(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _httpClient.GetAsync("api/Auth/userInfo");
            _httpClient.DefaultRequestHeaders.Authorization = null;

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

        public string GenerateQrCodeUri(string authenticatorUri)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new PngByteQRCode(qrCodeData))
                {
                    byte[] qrCodeBytes = qrCode.GetGraphic(20);
                    return "data:image/png;base64," + Convert.ToBase64String(qrCodeBytes);
                }
            }
        }

        // CRUD Methods for Admin
        public async Task<PagedResultDto<AccountResponseDto>> GetAccountsAsync(string accessToken, AccountFilterDto filter)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
            if (!string.IsNullOrEmpty(filter.Email)) query["Email"] = filter.Email;
            if (!string.IsNullOrEmpty(filter.Username)) query["Username"] = filter.Username;
            if (!string.IsNullOrEmpty(filter.Role)) query["Role"] = filter.Role;
            if (!string.IsNullOrEmpty(filter.IsActive)) query["IsActive"] = filter.IsActive;
            if (!string.IsNullOrEmpty(filter.AuthProvider)) query["AuthProvider"] = filter.AuthProvider;
            query["PageNumber"] = filter.PageNumber.ToString();
            query["PageSize"] = filter.PageSize.ToString();
            query["SortBy"] = filter.SortBy;
            query["SortOrder"] = filter.SortOrder;

            var response = await _httpClient.GetAsync($"api/admin/Account?{query}");
            _httpClient.DefaultRequestHeaders.Authorization = null;

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PagedResultDto<AccountResponseDto>>();
            }
            return new PagedResultDto<AccountResponseDto>();
        }

        public async Task<AccountResponseDto?> GetAccountDetailAsync(string accessToken, int id)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _httpClient.GetAsync($"api/admin/Account/{id}");
            _httpClient.DefaultRequestHeaders.Authorization = null;

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AccountResponseDto>();
            }
            return null;
        }

        public async Task<(bool Success, string Message, AccountResponseDto? Account)> CreateAccountAsync(string accessToken, AccountCreateDto dto)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/admin/Account", content);
            _httpClient.DefaultRequestHeaders.Authorization = null;

            if (response.IsSuccessStatusCode)
            {
                var account = await response.Content.ReadFromJsonAsync<AccountResponseDto>();
                return (true, "Tạo tài khoản thành công", account);
            }
            var error = await response.Content.ReadAsStringAsync();
            return (false, error, null);
        }

        public async Task<(bool Success, string Message)> UpdateAccountAsync(string accessToken, int id, AccountUpdateDto dto)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/admin/Account/{id}", content);
            _httpClient.DefaultRequestHeaders.Authorization = null;

            if (response.IsSuccessStatusCode)
            {
                return (true, "Cập nhật tài khoản thành công");
            }
            var error = await response.Content.ReadAsStringAsync();
            return (false, error);
        }

        public async Task<(bool Success, string Message)> DeleteAccountAsync(string accessToken, int id)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _httpClient.DeleteAsync($"api/admin/Account/{id}");
                _httpClient.DefaultRequestHeaders.Authorization = null;

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    try
                    {
                        // Sử dụng JsonDocument thay vì dynamic
                        using (var doc = JsonDocument.Parse(responseContent))
                        {
                            var message = doc.RootElement.GetProperty("Message").GetString() ?? "Xóa tài khoản thành công";
                            return (true, message);
                        }
                    }
                    catch
                    {
                        return (true, "Xóa tài khoản thành công");
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                string errorMessage = "Không thể xóa tài khoản";
                try
                {
                    using (var doc = JsonDocument.Parse(errorContent))
                    {
                        errorMessage = doc.RootElement.GetProperty("Message").GetString() ?? errorMessage;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing API response: {ex.Message}");
                }

                return (false, errorMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in DeleteAccountAsync: {ex.Message}");
                return (false, $"Có lỗi xảy ra: {ex.Message}");
            }
        }
        public async Task<(bool Success, string Message)> ToggleActiveAsync(string accessToken, int id)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _httpClient.PatchAsync($"api/admin/Account/{id}/toggle-active", null);
                _httpClient.DefaultRequestHeaders.Authorization = null;

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    try
                    {
                        using (var doc = JsonDocument.Parse(responseContent))
                        {
                            var message = doc.RootElement.GetProperty("Message").GetString() ?? "Thay đổi trạng thái thành công";
                            return (true, message);
                        }
                    }
                    catch
                    {
                        return (true, "Thay đổi trạng thái thành công");
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                string errorMessage = "Không thể thay đổi trạng thái tài khoản";
                try
                {
                    using (var doc = JsonDocument.Parse(errorContent))
                    {
                        errorMessage = doc.RootElement.GetProperty("Message").GetString() ?? errorMessage;
                    }
                }
                catch
                {
                    // Use default error message if JSON parsing fails
                }

                return (false, errorMessage);
            }
            catch (Exception ex)
            {
                return (false, $"Có lỗi xảy ra: {ex.Message}");
            }
        }
        public async Task<AccountStatistics> GetAccountStatisticsAsync(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _httpClient.GetAsync("api/admin/Account/statistics");
            _httpClient.DefaultRequestHeaders.Authorization = null;

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AccountStatistics>();
                return result ?? new AccountStatistics
                {
                    RoleDistribution = new[] { new RoleDistributionItem { Role = "Unknown", Count = 0 } },
                    AuthProviderDistribution = new[] { new AuthProviderDistributionItem { Provider = "Unknown", Count = 0 } }
                };
            }
            return new AccountStatistics
            {
                RoleDistribution = new[] { new RoleDistributionItem { Role = "Unknown", Count = 0 } },
                AuthProviderDistribution = new[] { new AuthProviderDistributionItem { Provider = "Unknown", Count = 0 } }
            };
        }
    }
}