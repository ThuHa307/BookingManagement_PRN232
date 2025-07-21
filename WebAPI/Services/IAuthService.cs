using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Domains;
using BusinessObjects.Dtos.Auth;

namespace WebAPI.Services
{
    public interface IAuthService
    {
        Task<Account?> RegisterAsync(RegisterDto model);
        Task<TokenResponseDto?> LoginAsync(LoginDto model);
        Task<TokenResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto request);
        Task<TokenResponseDto> GenerateTokenPair(Account user);
        public void setTokensInsideCookie(TokenResponseDto tokenDto, HttpContext context);
    }
}