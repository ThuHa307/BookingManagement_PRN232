using Microsoft.AspNetCore.Mvc;

namespace WebMVC.Helpers
{
    public static class AvatarHelper
    {
        /// <summary>
        /// Chuyển đổi relative avatar URL thành absolute URL
        /// </summary>
        /// <param name="avatarUrl">URL từ API</param>
        /// <param name="apiBaseUrl">Base URL của API</param>
        /// <returns>Absolute URL hoặc null</returns>
        public static string? GetFullAvatarUrl(string? avatarUrl, string apiBaseUrl)
        {
            if (string.IsNullOrEmpty(avatarUrl))
                return null;

            // Nếu đã là URL đầy đủ (bắt đầu với http)
            if (avatarUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return avatarUrl;
            }

            // Nếu là relative URL (bắt đầu với /)
            if (avatarUrl.StartsWith("/"))
            {
                return $"{apiBaseUrl.TrimEnd('/')}{avatarUrl}";
            }

            // Fallback
            return $"{apiBaseUrl.TrimEnd('/')}/{avatarUrl.TrimStart('/')}";
        }
    }
}