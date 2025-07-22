using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using BusinessObjects.Domains;
using BusinessObjects.Dtos.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OtpNet;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _config;

        private IAccountRepository _context;
        private readonly IJwtTokenGenerator _tokenGenerator;
        private readonly IPasswordHasherCustom _passwordHasher; // Sử dụng Custom Password Hasher
        private readonly UrlEncoder _urlEncoder;

        public AuthService(
            IAccountRepository accountRepository,
            IConfiguration config,
            IJwtTokenGenerator jwtTokenGenerator,
            IPasswordHasherCustom passwordHasher, // Inject PasswordHasherCustom
            UrlEncoder urlEncoder)
        {
            _config = config;
            _context = accountRepository;
            _tokenGenerator = jwtTokenGenerator;
            _passwordHasher = passwordHasher; // Gán
            _urlEncoder = urlEncoder;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto model)
        {
            var account = await _context.GetSystemAccountByEmailAsync(model.email!);
            if (account == null || !_passwordHasher.VerifyPassword(model.passwordHasher!, account.Password!)) // Dùng Custom Password Hasher
            {
                // TODO: Log failed login attempt, implement lockout logic
                return null; // Email hoặc mật khẩu không đúng
            }
            if (account.IsActive == "False")
            {
                // return specific error for inactive account if needed
                return null; // Tài khoản không hoạt động
            }

            if (account.TwoFactorEnabled == true)
            {
                // Trả về DTO báo hiệu cần 2FA
                return new LoginResponseDto { RequiresTwoFactor = true };
            }

            // Đăng nhập thành công, không cần 2FA
            return await GenerateTokenPair(account);
        }
        private string CreateToken(Account account)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email,account.Email),
                new Claim(ClaimTypes.NameIdentifier,account.AccountId.ToString()),
                new Claim(ClaimTypes.Role, account.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("Jwt:Key").Value!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new JwtSecurityToken(
                issuer: _config.GetSection("Jwt:Issuer").Value,
                audience: _config.GetSection("Jwt:Audience").Value,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }

        public async Task<LoginResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto request) // Đã đổi sang LoginResponseDto
        {
            if (string.IsNullOrEmpty(request.RefreshToken) || string.IsNullOrEmpty(request.Email))
            {
                return null;
            }

            var account = await _context.GetSystemAccountByEmailAsync(request.Email!);

            if (account == null || account.RefreshToken != request.RefreshToken || account.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return null;
            }

            return await GenerateTokenPair(account);
        }


        public async Task<Account?> RegisterAsync(AccountRegisterDto model) // <- Đổi từ RegisterDto thành AccountRegisterDto
        {
            // 1. Kiểm tra Email đã tồn tại chưa
            if (await _context.GetSystemAccountByEmailAsync(model.Email!) != null)
            {
                return null; // Trả về null hoặc ném một ngoại lệ cụ thể để frontend biết email đã tồn tại
            }

            // 2. Tạo đối tượng Account từ AccountRegisterDto
            Account account = new Account
            {
                Email = model.Email!,
                // Hash mật khẩu
                Password = _passwordHasher.HashPassword(model.Password!),
                // Sử dụng Role từ model thay vì gán cứng "U"
                Role = model.Role!, // <- Lấy giá trị Role từ DTO
                Username = model.Username!, // <- Lấy giá trị Username từ DTO
                IsActive = "A", // Giả sử "A" là Active
                AuthProvider = "Local", // Giả sử "Local" là đăng ký trực tiếp
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TwoFactorEnabled = false // Mặc định tắt 2FA khi đăng ký
            };

            // 3. Thêm tài khoản vào database
            await _context.AddAsync(account);
            await _context.SaveChangesAsync();

            // 4. Trả về tài khoản đã tạo
            return account;
        }

        private string GenerateRefreshToken()
        {
            var random = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(random);
            return Convert.ToBase64String(random);
        }
        private async Task<string> GenerateAndSaveRefreshToken(Account account)
        {
            var refreshToken = GenerateRefreshToken();
            account.RefreshToken = refreshToken;
            account.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();
            return refreshToken;
        }
        private async Task<Account?> ValidateRefreshTokenAsync(int accountId, string refreshToken)
        {
            var account = await _context.GetByIdAsync(accountId);
            if (account is null || account.RefreshToken != refreshToken)
            {
                return null;
            }
            return account;
        }
        public void setTokensInsideCookie(LoginResponseDto tokenDto, HttpContext context)
        {
            context.Response.Cookies.Append("accessToken", tokenDto.AccessToken!, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddMinutes(Convert.ToDouble(_config["Jwt:AccessTokenExpiresMinutes"])),
                HttpOnly = true,
                IsEssential = true,
                Secure = true,
                SameSite = SameSiteMode.Lax
            });
            context.Response.Cookies.Append("refreshToken", tokenDto.RefreshToken!, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(Convert.ToDouble(_config["Jwt:RefreshTokenExpiresDays"])),
                HttpOnly = true,
                IsEssential = true,
                Secure = true,
                SameSite = SameSiteMode.Lax
            });
        }
        public async Task<LoginResponseDto> GenerateTokenPair(Account account) // Đã đổi sang LoginResponseDto
        {
            var accessToken = _tokenGenerator.GenerateToken(account);
            var refreshToken = GenerateRefreshToken();

            account.RefreshToken = refreshToken;
            account.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(Convert.ToDouble(_config["Jwt:RefreshTokenExpiresDays"]));
            await _context.UpdateAsync(account);
            await _context.SaveChangesAsync();

            return new LoginResponseDto // Đã đổi sang LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Role = account.Role,
                RequiresTwoFactor = account.TwoFactorEnabled ?? false // Luôn set giá trị này, kể cả khi false
            };
        }
        // --- Các phương thức cho 2FA ---

        public string GenerateAuthenticatorKey()
        {
            var bytes = new byte[20];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Base32Encoding.ToString(bytes);
        }

        public string GetQrCodeUri(string email, string secretKey)
        {
            var appName = _urlEncoder.Encode(_config["AppName"] ?? "YourAppName");
            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6",
                appName,
                _urlEncoder.Encode(email),
                secretKey);
        }

        public bool VerifyTwoFactorCode(string secretKey, string code)
        {
            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(code))
            {
                return false;
            }

            try
            {
                var totp = new Totp(Base32Encoding.ToBytes(secretKey));
                long timeStepMatched = 0;

                // <<< SỬA ĐỔI DÒNG NÀY >>>
                // Sử dụng VerificationWindow(1, 1) để cho phép độ lệch 1 bước thời gian về trước và 1 bước thời gian về sau.
                // Điều này có nghĩa là mã sẽ hợp lệ nếu nó nằm trong 30 giây trước, 30 giây hiện tại hoặc 30 giây tiếp theo.
                return totp.VerifyTotp(
                    code.Replace(" ", string.Empty).Replace("-", string.Empty),
                    out timeStepMatched,
                    new VerificationWindow(1, 1) // <<< THÊM THAM SỐ NÀY
                );
            }
            catch (Exception ex)
            {
                // Log lỗi để dễ dàng gỡ lỗi hơn trong tương lai
                Console.WriteLine($"Error in VerifyTwoFactorCode: {ex.Message}");
                // Bạn cũng có thể dùng _logger.LogError(ex, "Lỗi khi xác minh mã 2FA.");
                return false;
            }
        }

        public IEnumerable<string> GenerateRecoveryCodes(int count = 10)
        {
            var codes = new List<string>();
            for (int i = 0; i < count; i++)
            {
                var bytes = new byte[8];
                using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                {
                    rng.GetBytes(bytes);
                }
                codes.Add(BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant().Substring(0, 8));
            }
            return codes;
        }

        public async Task<bool> VerifyRecoveryCodeAsync(Account account, string recoveryCode)
        {
            if (account == null || string.IsNullOrEmpty(account.RecoveryCodesJson))
            {
                return false;
            }

            var storedCodes = JsonSerializer.Deserialize<List<string>>(account.RecoveryCodesJson);
            if (storedCodes == null || !storedCodes.Contains(recoveryCode))
            {
                return false;
            }

            storedCodes.Remove(recoveryCode);
            account.RecoveryCodesJson = JsonSerializer.Serialize(storedCodes);
            await _context.UpdateAsync(account);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<TwoFactorStatusDto> GetTwoFactorStatusAsync(int accountId)
        {
            var account = await _context.GetByIdAsync(accountId);
            if (account == null) return new TwoFactorStatusDto { IsTwoFactorEnabled = false };

            return new TwoFactorStatusDto { IsTwoFactorEnabled = account.TwoFactorEnabled == true };
        }

        public async Task<EnableAuthenticatorDto?> GetTwoFactorSetupInfoAsync(int accountId)
        {
            var account = await _context.GetByIdAsync(accountId);
            if (account == null) return null;

            if (account.TwoFactorEnabled == true)
            {
                return null;
            }

            if (string.IsNullOrEmpty(account.AuthenticatorSecretKey))
            {
                account.AuthenticatorSecretKey = GenerateAuthenticatorKey();
                await _context.UpdateAsync(account);
                await _context.SaveChangesAsync();
            }

            var authenticatorUri = GetQrCodeUri(account.Email, account.AuthenticatorSecretKey);

            return new EnableAuthenticatorDto
            {
                SharedKey = FormatKey(account.AuthenticatorSecretKey),
                AuthenticatorUri = authenticatorUri
            };
        }

        public async Task<(bool success, string[]? recoveryCodes)> EnableTwoFactorAsync(int accountId, string verificationCode)
        {
            var account = await _context.GetByIdAsync(accountId);
            if (account == null || account.TwoFactorEnabled == true || string.IsNullOrEmpty(account.AuthenticatorSecretKey))
            {
                return (false, null);
            }

            if (!VerifyTwoFactorCode(account.AuthenticatorSecretKey, verificationCode))
            {
                return (false, null);
            }

            account.TwoFactorEnabled = true;
            var newRecoveryCodes = GenerateRecoveryCodes(10).ToArray();
            account.RecoveryCodesJson = JsonSerializer.Serialize(newRecoveryCodes);

            await _context.UpdateAsync(account);
            await _context.SaveChangesAsync();

            return (true, newRecoveryCodes);
        }

        public async Task<bool> DisableTwoFactorAsync(int accountId)
        {
            var account = await _context.GetByIdAsync(accountId);
            if (account == null)
            {
                return false;
            }

            account.TwoFactorEnabled = false;
            account.AuthenticatorSecretKey = null;
            account.RecoveryCodesJson = null;

            await _context.UpdateAsync(account);
            await _context.SaveChangesAsync();
            return true;
        }

        private string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            int currentPosition = 0;
            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
                currentPosition += 4;
            }
            if (currentPosition < unformattedKey.Length)
            {
                result.Append(unformattedKey.AsSpan(currentPosition));
            }
            return result.ToString().ToLowerInvariant();
        }

    }

}
