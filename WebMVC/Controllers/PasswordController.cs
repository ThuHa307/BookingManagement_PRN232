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
    public class PasswordController : Controller
    {
        private readonly PasswordApiService _passwordApiService;
        private readonly ILogger<PasswordController> _logger;

        public PasswordController(PasswordApiService passwordApiService, ILogger<PasswordController> logger)
        {
            _passwordApiService = passwordApiService;
            _logger = logger;
        }

        /// <summary>
        /// Hiển thị form đổi mật khẩu
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult ChangePassword()
        {
            // Kiểm tra đăng nhập
            var accessToken = HttpContext.Session.GetString("AccessToken");
            if (string.IsNullOrEmpty(accessToken))
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Auth");
            }

            return View(new ChangePasswordViewModel());
        }

        /// <summary>
        /// Xử lý đổi mật khẩu
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
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

                var (success, message) = await _passwordApiService.ChangePasswordAsync(accessToken, model);

                if (success)
                {
                    _logger.LogInformation("Password changed successfully for user");

                    // Xóa session hiện tại vì user cần đăng nhập lại
                    HttpContext.Session.Clear();

                    TempData["SuccessMessage"] = message + " Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }
                else
                {
                    // Xử lý các loại lỗi khác nhau
                    if (message.Contains("hết hạn") || message.Contains("Unauthorized") || message.Contains("không hợp lệ"))
                    {
                        // Thử refresh token
                        var newAccessToken = await _passwordApiService.RefreshTokenIfNeededAsync();
                        if (!string.IsNullOrEmpty(newAccessToken))
                        {
                            HttpContext.Session.SetString("AccessToken", newAccessToken);

                            // Thử lại với token mới
                            var (retrySuccess, retryMessage) = await _passwordApiService.ChangePasswordAsync(newAccessToken, model);
                            if (retrySuccess)
                            {
                                HttpContext.Session.Clear();
                                TempData["SuccessMessage"] = retryMessage + " Vui lòng đăng nhập lại.";
                                return RedirectToAction("Login", "Auth");
                            }
                            else
                            {
                                // Lỗi cụ thể từ API - dùng TempData cho SweetAlert
                                if (retryMessage.Contains("không đúng"))
                                {
                                    TempData["ErrorMessage"] = "Mật khẩu hiện tại không đúng. Vui lòng kiểm tra lại.";
                                }
                                else if (retryMessage.Contains("phải khác"))
                                {
                                    TempData["ErrorMessage"] = "Mật khẩu mới phải khác mật khẩu hiện tại.";
                                }
                                else
                                {
                                    TempData["ErrorMessage"] = retryMessage;
                                }

                                // Clear form fields
                                model.CurrentPassword = string.Empty;
                                model.NewPassword = string.Empty;
                                model.ConfirmPassword = string.Empty;

                                return View(model);
                            }
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                            return RedirectToAction("Login", "Auth");
                        }
                    }
                    else
                    {
                        // Phân loại lỗi cụ thể và dùng TempData để hiển thị SweetAlert
                        if (message.Contains("không đúng"))
                        {
                            TempData["ErrorMessage"] = "Mật khẩu hiện tại không đúng. Vui lòng kiểm tra lại.";
                        }
                        else if (message.Contains("phải khác"))
                        {
                            TempData["ErrorMessage"] = "Mật khẩu mới phải khác mật khẩu hiện tại.";
                        }
                        else if (message.Contains("không hợp lệ"))
                        {
                            TempData["ErrorMessage"] = "Thông tin không hợp lệ. Vui lòng kiểm tra lại.";
                        }
                        else
                        {
                            TempData["ErrorMessage"] = message;
                        }

                        // Clear form fields để user nhập lại
                        model.CurrentPassword = string.Empty;
                        model.NewPassword = string.Empty;
                        model.ConfirmPassword = string.Empty;

                        return View(model);
                    }
                }

                // Clear form fields trừ CurrentPassword để user không phải nhập lại
                model.NewPassword = string.Empty;
                model.ConfirmPassword = string.Empty;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing change password request");
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi đổi mật khẩu. Vui lòng thử lại.");

                // Clear sensitive fields
                model.CurrentPassword = string.Empty;
                model.NewPassword = string.Empty;
                model.ConfirmPassword = string.Empty;

                return View(model);
            }
        }
    }
}