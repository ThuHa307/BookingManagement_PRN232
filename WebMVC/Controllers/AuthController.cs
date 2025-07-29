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
using BusinessObjects.Consts;
using BusinessObjects.Dtos;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using BusinessObjects.Dtos.Response;

namespace WebMVC.Controllers
{
    [Route("[controller]")]
    public class AuthController : Controller
    {
        private readonly AuthApiService _apiService;
        private readonly AccountApiService _accountApiService;
        private readonly ILogger<AuthController> _logger;
        private readonly HttpClient _httpClient;

        public AuthController(AuthApiService service, ILogger<AuthController> logger, IHttpClientFactory httpClientFactory, AccountApiService accountApiService)
        {
            _apiService = service;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://localhost:5290");
            _accountApiService = accountApiService;
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
                        //
                        var userRole = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

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
                            // Kiểm tra vai trò và chuyển hướng
                            if (userRole == "A")
                            {
                                return RedirectToAction("Index", "Admin");
                            }
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
                    var userRoleFromToken = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                    var userIdFromToken = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                    HttpContext.Session.SetString("Email", userEmailFromToken!);
                    HttpContext.Session.SetString("Role", userRoleFromToken!);
                    HttpContext.Session.SetString("UserId", userIdFromToken!);
                    TempData["SuccessMessage"] = "Xác thực 2FA thành công!";

                    // Kiểm tra vai trò và chuyển hướng
                    if (userRoleFromToken == "A")
                    {
                        return RedirectToAction("Index", "Admin");
                    }
                    else { return RedirectToAction("Index", "Home"); }
                        

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

        [HttpGet("ExternalLogin")]
        public IActionResult ExternalLogin(string provider)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Auth", new { provider }, Request.Scheme);
            return Redirect($"https://localhost:5290/api/auth/external-login?provider={provider}&redirectUri={Uri.EscapeDataString(redirectUrl)}");
        }

        [HttpGet("ExternalLoginCallback")]
        public async Task<IActionResult> ExternalLoginCallback(string provider)
        {
            return Redirect($"https://localhost:5290/api/auth/external-login-callback?provider={provider}");
        }

        // ✅ THÊM ACTION MỚI để xử lý thành công external login
        [HttpGet("ExternalLoginSuccess")]
        public async Task<IActionResult> ExternalLoginSuccess(int accountId, string firstName, string lastName, string email, string provider, string accessToken, string refreshToken)
        {
            try
            {
                // ✅ TẠO CLAIMS CHO MVC APPLICATION
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, accountId.ToString()),
            new Claim(ClaimTypes.Name, $"{firstName} {lastName}".Trim()),
            new Claim(ClaimTypes.GivenName, firstName ?? ""),
            new Claim(ClaimTypes.Surname, lastName ?? ""),
            new Claim(ClaimTypes.Email, email),
            new Claim("Provider", provider)
        };
                // Lấy thông tin vai trò từ API hoặc từ dữ liệu khác (giả sử bạn có API để lấy vai trò)
                var userInfo = await _accountApiService.GetUserInfoAsync(accessToken);
                var role = userInfo?.Role ?? "U"; // Mặc định là User nếu không lấy được
                claims.Add(new Claim(ClaimTypes.Role, role));


                // ✅ SỬ DỤNG CookieAuthenticationDefaults.AuthenticationScheme thay vì AuthSchemes.Cookie
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
                    AllowRefresh = true
                };

                // ✅ SỬ DỤNG CookieAuthenticationDefaults.AuthenticationScheme
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

                // ✅ Set additional cookies trong MVC
                Response.Cookies.Append("AccountId", accountId.ToString(), new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTime.UtcNow.AddDays(7)
                });

                Response.Cookies.Append("AccountName", $"{firstName} {lastName}".Trim(), new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTime.UtcNow.AddDays(7)
                });

                Response.Cookies.Append("Email", email, new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTime.UtcNow.AddDays(7)
                });

                Response.Cookies.Append("LoginProvider", provider, new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTime.UtcNow.AddDays(7)
                });

                // ✅ Set session thông tin (backup)
                if (HttpContext.Session != null)
                {
                    HttpContext.Session.SetInt32("AccountId", accountId);
                    HttpContext.Session.SetString("AccountName", $"{firstName} {lastName}".Trim());
                    HttpContext.Session.SetString("Email", email);
                    HttpContext.Session.SetString("LoginProvider", provider);
                    HttpContext.Session.SetString("AccessToken", accessToken);
                    HttpContext.Session.SetString("RefreshToken", refreshToken);
                    HttpContext.Session.SetString("Role", role);

                }

                TempData["SuccessMessage"] = "Đăng nhập thành công! Đang chuyển hướng đến trang chủ...";
                // Chuyển hướng dựa trên vai trò
                if (role == "A")
                {
                    return RedirectToAction("Index", "Admin");
                }
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xử lý đăng nhập external");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi đăng nhập. Vui lòng thử lại.";
                return RedirectToAction("Login", "Auth");
            }
        }

        [HttpGet("AccessDenied")]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet("CompleteRegistration")]
        public IActionResult CompleteRegistration()
        {
            var dto = new ExternalAccountRegisterDto
            {
                Email = Request.Query["email"].ToString(),
                AuthProvider = Request.Query["provider"].ToString(),
                AuthProviderId = Request.Query["providerId"].ToString(), // Đảm bảo lấy đúng providerId
                FirstName = Request.Query["firstName"].ToString(),
                LastName = Request.Query["lastName"].ToString()
            };

            // Kiểm tra thông tin có đầy đủ không
            if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.AuthProvider) || string.IsNullOrEmpty(dto.AuthProviderId))
            {
                TempData["ErrorMessage"] = "Thiếu thông tin cần thiết để hoàn tất đăng ký. Vui lòng thử lại.";
                return RedirectToAction("Login", "Auth");
            }

            return View(dto);
        }

        [HttpPost("CompleteRegistration")]
        public async Task<IActionResult> CompleteRegistration(ExternalAccountRegisterDto dto)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            // Kiểm tra lại AuthProviderId trước khi gửi request
            if (string.IsNullOrEmpty(dto.AuthProviderId))
            {
                TempData["ErrorMessage"] = "Thiếu thông tin xác thực. Vui lòng thử đăng nhập lại.";
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/auth/complete-registration", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var registrationResponse = JsonSerializer.Deserialize<RegistrationResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    // Lưu thông tin vào session
                    if (HttpContext.Session != null)
                    {
                        HttpContext.Session.SetInt32("AccountId", registrationResponse.AccountId);
                        HttpContext.Session.SetString("AccountName", $"{dto.FirstName} {dto.LastName}".Trim());
                        HttpContext.Session.SetString("Email", registrationResponse.Email);
                        HttpContext.Session.SetString("LoginProvider", dto.AuthProvider ?? "");
                    }

                    TempData["SuccessRegisMessage"] = "Đăng ký tài khoản thành công! Đang chuyển hướng bạn đến trang chủ...";
                    TempData["RedirectUrl"] = registrationResponse.RedirectUrl;
                    return RedirectToAction("Index", "Home");
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Hoàn tất đăng ký thất bại: " + error);

                // Xử lý lỗi cụ thể
                if (error.Contains("AuthProviderId"))
                {
                    TempData["ErrorMessage"] = "Thông tin xác thực không hợp lệ. Vui lòng thử đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }

                TempData["ErrorMessage"] = "Không thể hoàn tất đăng ký. Vui lòng thử lại.";
                return View(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hoàn tất đăng ký");
                TempData["ErrorMessage"] = "Hệ thống đang gặp sự cố. Vui lòng thử lại sau.";
                return View(dto);
            }
        }
    }
}