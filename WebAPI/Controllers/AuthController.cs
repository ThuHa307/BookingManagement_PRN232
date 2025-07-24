using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using BusinessObjects.Consts;
using BusinessObjects.Domains;
using BusinessObjects.Dtos;
using BusinessObjects.Dtos.Auth;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly IAccountService _accountService;
        private readonly IPasswordHasherCustom _passwordHasher;
        private readonly IMailService _mailService;
        private readonly IConfiguration _config;

        public AuthController(IAuthService authService, IAccountService systemAccountService, ILogger<AuthController> logger,  IMailService mailService, IConfiguration config, IPasswordHasherCustom passwordHasher)
        {
            _authService = authService;
            _accountService = systemAccountService;
            _logger = logger;
            _passwordHasher = passwordHasher;
            _mailService = mailService;
            _config = config;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AccountRegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var account = await _authService.RegisterAsync(model);
            if (account == null)
            {
                return Conflict("Email đã được đăng ký.");
            }

            _logger.LogInformation($"Account '{model.Email}' registered successfully.");
            return Ok("Đăng ký tài khoản thành công.");
        }
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _authService.LoginAsync(model);

            if (response == null)
            {
                return Unauthorized("Email hoặc mật khẩu không đúng.");
            }

            if (response.RequiresTwoFactor)
            {
                HttpContext.Session.SetString("TwoFactorEmail", model.email!);
                _logger.LogInformation($"Email '{model.email}' stored in backend session for 2FA. Session ID: {HttpContext.Session.Id}");
                return Ok(response);
            }

            _authService.setTokensInsideCookie(response, HttpContext);
            return Ok(response);
        }
        [HttpGet("refresh-token")]
        public async Task<ActionResult<LoginResponseDto>> RefreshToken()
        {
            var refreshTokenFromCookie = Request.Cookies["refreshToken"];
            var accessTokenFromCookie = Request.Cookies["accessToken"];

            string? email = null;
            if (!string.IsNullOrEmpty(accessTokenFromCookie))
            {
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(accessTokenFromCookie);
                    email = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi đọc Access Token để lấy email trong Refresh Token flow.");
                }
            }

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(refreshTokenFromCookie))
            {
                return Unauthorized("Thông tin làm mới token không đầy đủ.");
            }

            var requestDto = new RefreshTokenRequestDto { RefreshToken = refreshTokenFromCookie, Email = email };
            var newTokens = await _authService.RefreshTokenAsync(requestDto);

            if (newTokens == null)
            {
                Response.Cookies.Delete("accessToken");
                Response.Cookies.Delete("refreshToken");
                return Unauthorized("Refresh token không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.");
            }

            _authService.setTokensInsideCookie(newTokens, HttpContext);
            return Ok(newTokens);
        }
        [HttpPost("login-2fa")]
        public async Task<ActionResult<LoginResponseDto>> LoginTwoFactor([FromBody] TwoFactorLoginDto model)
        {
            var email = HttpContext.Session.GetString("TwoFactorEmail");
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Two-factor authentication session expired or not initiated.");
                return Unauthorized("Phiên xác thực hai yếu tố đã hết hạn. Vui lòng đăng nhập lại.");
            }

            var account = await _accountService.GetAccountByEmailAsync(email);

            if (account == null || account.TwoFactorEnabled != true || string.IsNullOrEmpty(account.AuthenticatorSecretKey))
            {
                _logger.LogWarning($"Two-factor authentication failed for account '{email}'. Invalid state.");
                HttpContext.Session.Remove("TwoFactorEmail");
                return Unauthorized("Cấu hình xác thực hai yếu tố không hợp lệ. Vui lòng đăng nhập lại.");
            }

            var authenticatorCode = model.Code!.Replace(" ", string.Empty).Replace("-", string.Empty);

            if (!_authService.VerifyTwoFactorCode(account.AuthenticatorSecretKey, authenticatorCode))
            {
                _logger.LogWarning("Invalid authenticator code entered for account '{Email}'.", email);
                return Unauthorized("Mã xác thực không đúng.");
            }

            _logger.LogInformation("Account '{Email}' logged in with 2FA.", email);

            HttpContext.Session.Remove("TwoFactorEmail");

            var loginResponse = await _authService.GenerateTokenPair(account);
            HttpContext.Session.SetString("AccessToken", loginResponse.AccessToken!);
            HttpContext.Session.SetString("RefreshToken", loginResponse.RefreshToken!);
            _authService.setTokensInsideCookie(loginResponse, HttpContext);
            return Ok(loginResponse);
        }
        [HttpPost("login-recovery-code")]
        public async Task<ActionResult<LoginResponseDto>> LoginWithRecoveryCode([FromBody] TwoFactorRecoveryCodeLoginDto model)
        {
            var email = HttpContext.Session.GetString("TwoFactorEmail");
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized("Phiên xác thực đã hết hạn. Vui lòng đăng nhập lại.");
            }

            var account = await _accountService.GetAccountByEmailAsync(email);

            if (account == null || account.TwoFactorEnabled != true || string.IsNullOrEmpty(account.RecoveryCodesJson))
            {
                return Unauthorized("Cấu hình xác thực hai yếu tố không hợp lệ.");
            }

            if (!await _authService.VerifyRecoveryCodeAsync(account, model.RecoveryCode))
            {
                return Unauthorized("Mã khôi phục không hợp lệ.");
            }

            HttpContext.Session.Remove("TwoFactorEmail");
            var loginResponse = await _authService.GenerateTokenPair(account);
            _authService.setTokensInsideCookie(loginResponse, HttpContext);
            return Ok(loginResponse);
        }
        [HttpGet("2fa-status")]
        public async Task<ActionResult<TwoFactorStatusDto>> GetTwoFactorStatus()
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (accountIdClaim == null || !int.TryParse(accountIdClaim.Value, out int accountId))
            {
                return Unauthorized("Thông tin người dùng không hợp lệ.");
            }
            return await _authService.GetTwoFactorStatusAsync(accountId);
        }
        [HttpGet("2fa-setup-info")]

        public async Task<ActionResult<EnableAuthenticatorDto>> GetTwoFactorSetupInfo()
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (accountIdClaim == null || !int.TryParse(accountIdClaim.Value, out int accountId))
            {
                return Unauthorized("Thông tin người dùng không hợp lệ.");
            }

            var setupInfo = await _authService.GetTwoFactorSetupInfoAsync(accountId);
            if (setupInfo == null)
            {
                var status = await _authService.GetTwoFactorStatusAsync(accountId);
                if (status.IsTwoFactorEnabled)
                {
                    return BadRequest("Xác thực hai yếu tố đã được bật cho tài khoản này.");
                }
                return NotFound("Không tìm thấy tài khoản hoặc lỗi cấu hình.");
            }
            return Ok(setupInfo);
        }


        [HttpPost("2fa-enable")]
        public async Task<IActionResult> EnableTwoFactor([FromBody] TwoFactorVerificationDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (accountIdClaim == null || !int.TryParse(accountIdClaim.Value, out int accountId))
            {
                return Unauthorized("Thông tin người dùng không hợp lệ.");
            }

            var (success, recoveryCodes) = await _authService.EnableTwoFactorAsync(accountId, model.Code);
            if (!success)
            {
                return BadRequest("Mã xác minh không đúng hoặc đã có lỗi khi bật 2FA. Vui lòng thử lại.");
            }

            return Ok(new GeneratedRecoveryCodesDto { RecoveryCodes = recoveryCodes! });
        }

        [HttpPost("2fa-disable")]
        public async Task<IActionResult> DisableTwoFactor()
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (accountIdClaim == null || !int.TryParse(accountIdClaim.Value, out int accountId))
            {
                return Unauthorized("Thông tin người dùng không hợp lệ.");
            }

            var success = await _authService.DisableTwoFactorAsync(accountId);
            if (!success)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, "Không thể tắt xác thực hai yếu tố. Vui lòng thử lại.");
            }

            return Ok("Xác thực hai yếu tố đã được tắt thành công.");
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (accountIdClaim != null && int.TryParse(accountIdClaim.Value, out int accountId))
            {
                var account = await _accountService.GetAccountByIdAsync(accountId);
                if (account != null)
                {
                    account.RefreshToken = null;
                    account.RefreshTokenExpiryTime = null;
                    await _accountService.UpdateAccountAsync(accountId, account);
                }
            }

            Response.Cookies.Delete("accessToken");
            Response.Cookies.Delete("refreshToken");
            return Ok("Đăng xuất thành công.");
        }

        [HttpGet("userInfo")]
        public IActionResult GetCurrentUser()
        {

            var identity = HttpContext.User.Identity as ClaimsIdentity;
            if (identity != null)
            {
                var userId = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var email = identity.FindFirst(ClaimTypes.Email)?.Value;
                var role = identity.FindFirst(ClaimTypes.Role)?.Value;

                return Ok(new
                {
                    UserId = userId,
                    Email = email,
                    Role = role
                });
            }
            return Unauthorized();
        }
        //Change Password
        [HttpPost("change-password")]
        public async Task<ActionResult<ChangePasswordResponseDto>> ChangePassword([FromBody] ChangePasswordDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ChangePasswordResponseDto
                    {
                        Success = false,
                        Message = "Dữ liệu không hợp lệ"
                    });
                }

                // Lấy thông tin user hiện tại từ token
                var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (accountIdClaim == null || !int.TryParse(accountIdClaim.Value, out int accountId))
                {
                    return Unauthorized(new ChangePasswordResponseDto
                    {
                        Success = false,
                        Message = "Thông tin người dùng không hợp lệ"
                    });
                }

                // Lấy account từ database
                var account = await _accountService.GetAccountByIdAsync(accountId);
                if (account == null)
                {
                    return NotFound(new ChangePasswordResponseDto
                    {
                        Success = false,
                        Message = "Không tìm thấy tài khoản"
                    });
                }

                // Kiểm tra mật khẩu hiện tại
                if (!_passwordHasher.VerifyPassword(model.CurrentPassword, account.Password!))
                {
                    return BadRequest(new ChangePasswordResponseDto
                    {
                        Success = false,
                        Message = "Mật khẩu hiện tại không đúng"
                    });
                }

                // Kiểm tra mật khẩu mới không trùng mật khẩu cũ
                if (_passwordHasher.VerifyPassword(model.NewPassword, account.Password!))
                {
                    return BadRequest(new ChangePasswordResponseDto
                    {
                        Success = false,
                        Message = "Mật khẩu mới phải khác mật khẩu hiện tại"
                    });
                }

                // Cập nhật mật khẩu mới
                account.Password = _passwordHasher.HashPassword(model.NewPassword);
                account.UpdatedAt = DateTime.UtcNow;

                // Vô hiệu hóa tất cả refresh token (buộc đăng nhập lại trên tất cả thiết bị)
                account.RefreshToken = null;
                account.RefreshTokenExpiryTime = null;

                await _accountService.UpdateAccountAsync(accountId, account);

                _logger.LogInformation("Password changed successfully for account {AccountId}", accountId);

                return Ok(new ChangePasswordResponseDto
                {
                    Success = true,
                    Message = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for account");
                return StatusCode(500, new ChangePasswordResponseDto
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi đổi mật khẩu"
                });
            }
        }

        [HttpGet("external-login")]
        public IActionResult ExternalLogin(string provider, string redirectUri)
        {
            var scheme = provider.ToLower() switch
            {
                AuthProviders.Google => AuthSchemes.Google,
                AuthProviders.Facebook => AuthSchemes.Facebook,
                _ => throw new ArgumentException("Provider không hợp lệ", nameof(provider))
            };

            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUri
            };

            return Challenge(properties, scheme);
        }

        [HttpGet("external-login-callback")]
        public async Task<IActionResult> ExternalLoginCallback(string provider)
        {
            var scheme = provider.ToLower() switch
            {
                AuthProviders.Google => AuthSchemes.Google,
                AuthProviders.Facebook => AuthSchemes.Facebook,
                _ => throw new ArgumentException("Provider không hợp lệ", nameof(provider))
            };

            var result = await HttpContext.AuthenticateAsync(scheme);
            if (!result.Succeeded)
            {
                return BadRequest("Xác thực không thành công.");
            }

            var principal = result.Principal;
            var externalId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = principal.FindFirst(ClaimTypes.Name)?.Value;
            var firstName = principal.FindFirst(ClaimTypes.GivenName)?.Value;
            var lastName = principal.FindFirst(ClaimTypes.Surname)?.Value;

            if (string.IsNullOrEmpty(externalId) || string.IsNullOrEmpty(email))
            {
                return BadRequest("Tài khoản không hợp lệ.");
            }

            var account = await _accountService.GetAccountByEmailAsync(email);
            if (account == null)
            {
                // Chuyển hướng đến trang đăng ký
                var redirectUrl = $"https://localhost:7031/Auth/CompleteRegistration?" +
                                  $"email={Uri.EscapeDataString(email)}" +
                                  $"&firstName={Uri.EscapeDataString(firstName ?? name)}" +
                                  $"&lastName={Uri.EscapeDataString(lastName ?? "")}" +
                                  $"&provider={provider}" +
                                  $"&providerId={Uri.EscapeDataString(externalId)}";
                return Redirect(redirectUrl);
            }

            // ✅ TẠO TOKEN CHO USER ĐÃ TỒN TẠI
            var tokenResponse = await _authService.GenerateTokenForExternalLoginAsync(account);
            if (tokenResponse == null)
            {
                return BadRequest("Không thể tạo token xác thực.");
            }

            // ✅ Set tokens vào cookies
            _authService.setTokensInsideCookie(tokenResponse, HttpContext);

            // ✅ TẠO AUTHENTICATION COOKIE CHO MVC APPLICATION (QUAN TRỌNG!)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, account.AccountId.ToString()),
                new Claim(ClaimTypes.Name, $"{firstName ?? ""} {lastName ?? ""}".Trim()),
                new Claim(ClaimTypes.GivenName, firstName ?? ""),
                new Claim(ClaimTypes.Surname, lastName ?? ""),
                new Claim(ClaimTypes.Email, account.Email),
                new Claim(ClaimTypes.Role, account.Role),
                new Claim("Provider", provider) // Thêm provider claim
            };

            var identity = new ClaimsIdentity(claims, AuthSchemes.Cookie); // Sử dụng Cookie scheme
            var claimsPrincipal = new ClaimsPrincipal(identity);

            // Set authentication cookie với thời gian hết hạn
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7), // 7 ngày
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(AuthSchemes.Cookie, claimsPrincipal, authProperties);

            // ✅ REDIRECT TRỰC TIẾP VỀ MVC CONTROLLER để xử lý cookies
            var callbackUrl = $"https://localhost:7031/Auth/ExternalLoginSuccess?" +
                             $"accountId={account.AccountId}" +
                             $"&firstName={Uri.EscapeDataString(firstName ?? "")}" +
                             $"&lastName={Uri.EscapeDataString(lastName ?? "")}" +
                             $"&email={Uri.EscapeDataString(account.Email)}" +
                             $"&provider={Uri.EscapeDataString(provider)}" +
                             $"&accessToken={Uri.EscapeDataString(tokenResponse.AccessToken ?? "")}" +
                             $"&refreshToken={Uri.EscapeDataString(tokenResponse.RefreshToken ?? "")}";

            return Redirect(callbackUrl);
        }

        [HttpPost("complete-registration")]
        public async Task<IActionResult> CompleteRegistration([FromBody] ExternalAccountRegisterDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Kiểm tra các thông tin cần thiết
            if (string.IsNullOrEmpty(dto.AuthProviderId))
            {
                return BadRequest(new { ErrorMessage = "AuthProviderId không được để trống." });
            }

            if (string.IsNullOrEmpty(dto.Email))
            {
                return BadRequest(new { ErrorMessage = "Email không được để trống." });
            }

            if (string.IsNullOrEmpty(dto.FirstName))
            {
                return BadRequest(new { ErrorMessage = "FirstName không được để trống." });
            }

            try
            {
                // Không dựa vào HttpContext.AuthenticateAsync nữa
                // Thay vào đó, sử dụng thông tin đã được truyền từ client
                var account = await _accountService.CreateExternalAccountAsync(dto);

                if (account == null)
                {
                    return BadRequest(new { ErrorMessage = "Không thể tạo tài khoản. Vui lòng thử lại." });
                }

                // Tạo claims cho user mới
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, account.AccountId.ToString()),
                    new Claim(ClaimTypes.Name, $"{dto.FirstName} {dto.LastName}".Trim()),
                    new Claim(ClaimTypes.GivenName, dto.FirstName ?? ""),
                    new Claim(ClaimTypes.Surname, dto.LastName ?? ""),
                    new Claim(ClaimTypes.Email, dto.Email),
                    new Claim(ClaimTypes.Role, account.Role)
                };

                var scheme = dto.AuthProvider.ToLower() switch
                {
                    "google" => AuthSchemes.Google,
                    "facebook" => AuthSchemes.Facebook,
                    _ => AuthSchemes.Cookie // fallback to cookie scheme
                };

                var identity = new ClaimsIdentity(claims, scheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(AuthSchemes.Cookie, principal);

                // Gửi email welcome (với try-catch để tránh lỗi email làm crash app)
                try
                {
                    var baseUrl = _config["AppSettings:BaseUrl"];
                    var mail = new MailContent
                    {
                        To = dto.Email,
                        Subject = "Chào mừng đến với BlueHouse!",
                        Body = $@"
                            <div style='font-family:Arial,sans-serif;padding:20px;color:#333'>
                                <h2>Chào mừng {dto.FirstName} {dto.LastName} đến với <span style='color:#007bff;'>BlueHouse</span>!</h2>
                                <p>Cảm ơn bạn đã đăng ký tài khoản bằng {dto.AuthProvider}. Chúng tôi rất vui khi có bạn là một phần của cộng đồng.</p>
                                <img src='https://localhost:7046/images/welcome-mail.jpg' alt='Welcome' style='max-width:100%;border-radius:10px;margin:20px 0' />
                                <p>Bắt đầu hành trình của bạn ngay hôm nay bằng cách khám phá các tính năng tuyệt vời của BlueHouse.</p>
                                <a href='{baseUrl}' style='display:inline-block;padding:10px 20px;background:#4b69bd;color:#fff;text-decoration:none;border-radius:5px;'>Truy cập BlueHouse</a>
                            </div>"
                    };

                    await _mailService.SendMail(mail);
                }
                catch (Exception emailEx)
                {
                    // Log lỗi email nhưng không làm crash registration process
                    // Có thể log: _logger.LogError(emailEx, "Failed to send welcome email to {Email}", dto.Email);
                    // Không return lỗi vì account đã được tạo thành công
                }

                return Ok(new
                {
                    AccountId = account.AccountId,
                    Email = account.Email,
                    Role = account.Role,
                    RedirectUrl = Url.Action("Index", "Home", null, Request.Scheme)
                });
            }
            catch (Exception ex)
            {
                // Log lỗi và return generic error
                // _logger.LogError(ex, "Error completing registration for {Email}", dto.Email);
                return BadRequest(new { ErrorMessage = "Có lỗi xảy ra khi tạo tài khoản. Vui lòng thử lại." });
            }
        }

    }
}