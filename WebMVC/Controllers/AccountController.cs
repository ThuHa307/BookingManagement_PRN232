using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebMVC.Models;
using WebMVC.API;

namespace WebMVC.Controllers
{
    [Route("[controller]")]
    public class AccountController : Controller
    {
        private readonly AccountApiService _apiService;
        public AccountController(AccountApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet("TwoFactorSetup")]
        public async Task<IActionResult> TwoFactorSetup()
        {
            var accessToken = HttpContext.Session.GetString("AccessToken");
            if (string.IsNullOrEmpty(accessToken))
            {
                return RedirectToAction("Login", "Auth");
            }

            var twoFactorStatus = await _apiService.GetTwoFactorStatusAsync(accessToken);
            if (twoFactorStatus?.IsTwoFactorEnabled == true)
            {
                TempData["ErrorMessage"] = "Xác thực hai yếu tố đã được bật.";
                return RedirectToAction("Index", "Home");
            }

            var setupInfo = await _apiService.GetTwoFactorSetupInfoAsync(accessToken);
            if (setupInfo == null)
            {
                TempData["ErrorMessage"] = "Không thể lấy thông tin cài đặt 2FA.";
                return RedirectToAction("Profile");
            }

            ViewBag.SharedKey = setupInfo.SharedKey;
            ViewBag.AuthenticatorUri = setupInfo.AuthenticatorUri;
            ViewBag.QrCodeUrl = _apiService.GenerateQrCodeUri(setupInfo.AuthenticatorUri);

            return View();
        }

        [HttpPost("EnableTwoFactor")]
        public async Task<IActionResult> EnableTwoFactor(TwoFactorVerificationViewModel model)
        {
            var accessToken = HttpContext.Session.GetString("AccessToken");
            if (string.IsNullOrEmpty(accessToken))
            {
                return RedirectToAction("Login", "Auth");
            }

            if (ModelState.IsValid)
            {
                var response = await _apiService.EnableTwoFactorAsync(accessToken, model);
                if (response != null && response.RecoveryCodes.Any())
                {
                    TempData["SuccessMessage"] = "Đăng nhập thành công!";
                    var handler = new JwtSecurityTokenHandler();
                    var jsonToken = handler.ReadToken(accessToken) as JwtSecurityToken;

                    var identity = new ClaimsIdentity(jsonToken!.Claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = jsonToken.ValidTo.ToUniversalTime(),
                        AllowRefresh = true
                    });
                    var userEmailFromToken = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                    HttpContext.Session.SetString("Email", userEmailFromToken!);
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, "Mã xác thực không hợp lệ. Vui lòng kiểm tra lại.");
            }

            var setupInfo = await _apiService.GetTwoFactorSetupInfoAsync(accessToken);
            if (setupInfo != null)
            {
                ViewBag.SharedKey = setupInfo.SharedKey;
                ViewBag.AuthenticatorUri = setupInfo.AuthenticatorUri;
                ViewBag.QrCodeUrl = _apiService.GenerateQrCodeUri(setupInfo.AuthenticatorUri);
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(accessToken) as JwtSecurityToken;

                var identity = new ClaimsIdentity(jsonToken!.Claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = jsonToken.ValidTo.ToUniversalTime(),
                    AllowRefresh = true
                });
                var userEmailFromToken = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                HttpContext.Session.SetString("Email", userEmailFromToken!);

            }
            TempData["SuccessMessage"] = "Đăng nhập thành công!";
            return RedirectToAction("Index", "Home");
        }
        [HttpGet("Error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}