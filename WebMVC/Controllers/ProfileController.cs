using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebMVC.API;
using WebMVC.Models;

namespace WebMVC.Controllers
{
    public class ProfileController : Controller
    {
        private readonly ProfileApiService _profileApiService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(ProfileApiService profileApiService, ILogger<ProfileController> logger)
        {
            _profileApiService = profileApiService;
            _logger = logger;
        }

        /// <summary>
        /// Hiển thị trang profile
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            try
            {
                var accessToken = HttpContext.Session.GetString("AccessToken");
                if (string.IsNullOrEmpty(accessToken))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }

                var profile = await _profileApiService.GetMyProfileAsync(accessToken);

                // Nếu profile chưa tồn tại, tạo model rỗng để user có thể điền thông tin
                if (profile == null)
                {
                    profile = new ProfileViewModel();
                    ViewBag.IsNewProfile = true;
                }
                else
                {
                    ViewBag.IsNewProfile = false;
                }

                return View(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang profile.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Hiển thị form chỉnh sửa profile
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Edit()
        {
            try
            {
                var accessToken = HttpContext.Session.GetString("AccessToken");
                if (string.IsNullOrEmpty(accessToken))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }

                var profile = await _profileApiService.GetMyProfileAsync(accessToken);

                // Nếu profile chưa tồn tại, tạo model rỗng
                if (profile == null)
                {
                    profile = new ProfileViewModel();
                    ViewBag.IsNewProfile = true;
                }
                else
                {
                    ViewBag.IsNewProfile = false;
                }

                return View(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit profile page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang chỉnh sửa profile.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Xử lý cập nhật profile
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var accessToken = HttpContext.Session.GetString("AccessToken");
                if (string.IsNullOrEmpty(accessToken))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }

                var success = await _profileApiService.CreateOrUpdateProfileAsync(accessToken, model);

                if (success)
                {
                    // Cập nhật tên user trong session nếu có
                    if (!string.IsNullOrEmpty(model.FirstName) || !string.IsNullOrEmpty(model.LastName))
                    {
                        var fullName = $"{model.FirstName} {model.LastName}".Trim();
                        if (!string.IsNullOrEmpty(fullName))
                        {
                            HttpContext.Session.SetString("UserName", fullName);
                        }
                    }

                    TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công!";
                    return RedirectToAction("Index");
                }
                else
                {
                    // Thử refresh token và thử lại
                    var newAccessToken = await _profileApiService.RefreshTokenIfNeededAsync();
                    if (!string.IsNullOrEmpty(newAccessToken))
                    {
                        HttpContext.Session.SetString("AccessToken", newAccessToken);
                        success = await _profileApiService.CreateOrUpdateProfileAsync(newAccessToken, model);

                        if (success)
                        {
                            TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công!";
                            return RedirectToAction("Index");
                        }
                    }

                    TempData["ErrorMessage"] = "Không thể cập nhật thông tin. Vui lòng thử lại.";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật thông tin.";
                return View(model);
            }
        }

        /// <summary>
        /// Upload avatar
        /// </summary>
        /// <param name="avatarFile"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile avatarFile)
        {
            try
            {
                if (avatarFile == null || avatarFile.Length == 0)
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn file ảnh hợp lệ.";
                    return RedirectToAction("Edit");
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["ErrorMessage"] = "Chỉ chấp nhận file ảnh (.jpg, .jpeg, .png, .gif).";
                    return RedirectToAction("Edit");
                }

                // Validate file size (5MB)
                if (avatarFile.Length > 5 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "Kích thước file không được vượt quá 5MB.";
                    return RedirectToAction("Edit");
                }

                var accessToken = HttpContext.Session.GetString("AccessToken");
                if (string.IsNullOrEmpty(accessToken))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }

                var avatarUrl = await _profileApiService.UploadAvatarAsync(accessToken, avatarFile);

                if (!string.IsNullOrEmpty(avatarUrl))
                {
                    TempData["SuccessMessage"] = "Cập nhật ảnh đại diện thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể upload ảnh đại diện. Vui lòng thử lại.";
                }

                return RedirectToAction("Edit");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi upload ảnh đại diện.";
                return RedirectToAction("Edit");
            }
        }

        /// <summary>
        /// Xóa avatar
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAvatar()
        {
            try
            {
                var accessToken = HttpContext.Session.GetString("AccessToken");
                if (string.IsNullOrEmpty(accessToken))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }

                var success = await _profileApiService.DeleteAvatarAsync(accessToken);

                if (success)
                {
                    TempData["SuccessMessage"] = "Xóa ảnh đại diện thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể xóa ảnh đại diện. Vui lòng thử lại.";
                }

                return RedirectToAction("Edit");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting avatar");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa ảnh đại diện.";
                return RedirectToAction("Edit");
            }
        }

        /// <summary>
        /// API endpoint cho AJAX upload avatar
        /// </summary>
        /// <param name="avatarFile"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> UploadAvatarAjax(IFormFile avatarFile)
        {
            try
            {
                if (avatarFile == null || avatarFile.Length == 0)
                {
                    return Json(new { success = false, message = "Vui lòng chọn file ảnh hợp lệ." });
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return Json(new { success = false, message = "Chỉ chấp nhận file ảnh (.jpg, .jpeg, .png, .gif)." });
                }

                if (avatarFile.Length > 5 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "Kích thước file không được vượt quá 5MB." });
                }

                var accessToken = HttpContext.Session.GetString("AccessToken");
                if (string.IsNullOrEmpty(accessToken))
                {
                    return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn." });
                }

                var avatarUrl = await _profileApiService.UploadAvatarAsync(accessToken, avatarFile);

                if (!string.IsNullOrEmpty(avatarUrl))
                {
                    return Json(new { success = true, message = "Upload ảnh đại diện thành công!", avatarUrl = avatarUrl });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể upload ảnh đại diện." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar via AJAX");
                return Json(new { success = false, message = "Có lỗi xảy ra khi upload ảnh." });
            }
        }
    }
}