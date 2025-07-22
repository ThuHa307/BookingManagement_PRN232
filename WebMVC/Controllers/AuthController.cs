using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BusinessObjects.Dtos.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebMVC.Models;
using WebMVC.API;

namespace WebMVC.Controllers
{
    [Route("[controller]")]
    public class AuthController : Controller
    {
        private readonly AuthApiService _apiService;

        public AuthController(AuthApiService service)
        {
            _apiService = service;
        }

        [HttpGet("Register")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(AccountRegisterDto model)
        {
            if (ModelState.IsValid)
            {
                var result = await _apiService.RegisterAsync(model);
                if (result != null)
                {
                    TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                    return RedirectToAction("Login");
                }
                ModelState.AddModelError(string.Empty, "Đăng ký thất bại. Email hoặc tên người dùng đã tồn tại.");
            }
            return View(model);
        }

        [HttpGet("Login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginDto model)
        {
            if (ModelState.IsValid)
            {
                var loginResponse = await _apiService.LoginAsync(model);

                if (loginResponse != null)
                {
                    if (loginResponse.RequiresTwoFactor)
                    {
                        TempData["Email"] = model.email;
                        return RedirectToAction("TwoFactorVerification", new { email = model.email });
                    }
                    else if (!string.IsNullOrEmpty(loginResponse.AccessToken))
                    {
                        var handler = new JwtSecurityTokenHandler();
                        var jsonToken = handler.ReadToken(loginResponse.AccessToken) as JwtSecurityToken;

                        if (jsonToken == null)
                        {
                            ModelState.AddModelError(string.Empty, "Token không hợp lệ.");
                            return View(model);
                        }

                        var identity = new ClaimsIdentity(jsonToken.Claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var principal = new ClaimsPrincipal(identity);

                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = jsonToken.ValidTo.ToUniversalTime(),
                            AllowRefresh = true
                        });

                        HttpContext.Session.SetString("RefreshToken", loginResponse.RefreshToken ?? string.Empty);
                        HttpContext.Session.SetString("AccessToken", loginResponse.AccessToken ?? string.Empty);

                        var twoFactorStatus = await _apiService.GetTwoFactorStatusAsync(loginResponse.AccessToken!);

                        if (twoFactorStatus != null && !twoFactorStatus.IsTwoFactorEnabled)
                        {
                            TempData["Email"] = model.email;
                            TempData["SuccessMessage"] = "Đăng nhập thành công! Vui lòng thiết lập Xác thực hai yếu tố.";
                            return RedirectToAction("TwoFactorSetup", "Account", new { email = model.email });
                        }
                        else
                        {
                            TempData["SuccessMessage"] = "Đăng nhập thành công!";
                            return RedirectToAction("Index", "Home");
                        }
                    }
                }
                TempData["ErrorMessage"] = "Email hoặc mật khẩu không đúng. Vui lòng thử lại.";
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
            }
            return View(model);
        }

        [HttpGet("TwoFactorVerification")]
        public IActionResult TwoFactorVerification(string email)
        {
            ViewBag.Email = email;
            return View();
        }
        [HttpPost("TwoFactorVerification")]
        public async Task<IActionResult> TwoFactorVerification(TwoFactorLoginDto model, string email)
        {
            if (ModelState.IsValid)
            {

                var loginResponse = await _apiService.LoginTwoFactorAsync(model);
                if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.AccessToken))
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jsonToken = handler.ReadToken(loginResponse.AccessToken) as JwtSecurityToken;

                    var identity = new ClaimsIdentity(jsonToken!.Claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = jsonToken.ValidTo.ToUniversalTime(),
                        AllowRefresh = true
                    });

                    HttpContext.Session.SetString("RefreshToken", loginResponse.RefreshToken ?? string.Empty);
                    HttpContext.Session.SetString("AccessToken", loginResponse.AccessToken ?? string.Empty);
                    var userEmailFromToken = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                    HttpContext.Session.SetString("Email", userEmailFromToken!);
                    TempData["SuccessMessage"] = "Xác thực 2FA thành công!";
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, "Mã xác thực không đúng.");
            }
            return View(model);
        }
        [HttpGet("Logout")]
        public async Task<IActionResult> Logout()
        {
            await _apiService.LogoutAsync();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Đăng xuất thành công.";
            return RedirectToAction("Login");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}