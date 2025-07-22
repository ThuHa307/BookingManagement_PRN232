using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Domains;
using BusinessObjects.Dtos.Auth;
using Microsoft.AspNetCore.Http;

namespace Services.Interfaces
{
    public interface IAuthService
    {
        Task<Account?> RegisterAsync(AccountRegisterDto model);
        Task<LoginResponseDto?> LoginAsync(LoginDto model);
        Task<LoginResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto request);
        Task<LoginResponseDto> GenerateTokenPair(Account user);
        public void setTokensInsideCookie(LoginResponseDto tokenDto, HttpContext context);
        // --- Các phương thức mới cho 2FA ---
        string GenerateAuthenticatorKey(); // Tạo secret key cho authenticator
        string GetQrCodeUri(string email, string secretKey); // Tạo URI cho QR code
        bool VerifyTwoFactorCode(string secretKey, string code); // Xác minh mã 2FA từ app
        IEnumerable<string> GenerateRecoveryCodes(int count = 10); // Tạo mã khôi phục
        Task<bool> VerifyRecoveryCodeAsync(Account account, string recoveryCode); // Xác minh và vô hiệu hóa mã khôi phục

        Task<TwoFactorStatusDto> GetTwoFactorStatusAsync(int accountId);
        Task<EnableAuthenticatorDto?> GetTwoFactorSetupInfoAsync(int accountId);
        Task<(bool success, string[]? recoveryCodes)> EnableTwoFactorAsync(int accountId, string verificationCode);
        Task<bool> DisableTwoFactorAsync(int accountId);
        // --- END Các phương thức mới cho 2FA ---
    }
}