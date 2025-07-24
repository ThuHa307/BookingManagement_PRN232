using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using BusinessObjects.Domains;
using BusinessObjects.Dtos.Auth;
using Microsoft.AspNetCore.Authorization;
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
        public AuthController(IAuthService authService, IAccountService systemAccountService, ILogger<AuthController> logger, IPasswordHasherCustom passwordHasher)
        {
            _authService = authService;
            _accountService = systemAccountService;
            _logger = logger;
            _passwordHasher = passwordHasher;
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

    }
}