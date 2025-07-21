using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BusinessObjects.Domains;
using BusinessObjects.Dtos.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Repositories.Interfaces;
using Services.Interfaces;

namespace WebAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _config;

        private IAccountRepository _context;
        private readonly IJwtTokenGenerator _tokenGenerator;

        public AuthService(IAccountRepository systemAccountRepository, IConfiguration config, IJwtTokenGenerator jwtTokenGenerator)
        {
            _config = config;
            _context = systemAccountRepository;
            _tokenGenerator = jwtTokenGenerator;
        }

        public async Task<TokenResponseDto?> LoginAsync(LoginDto model)
        {
            var account = await _context.GetSystemAccountByEmailAsync(model.email!);
            if (account == null)
            {
                return null!;
            }
            if (!account.Email.Equals(model.email))
            {
                return null!;
            }
            else if (new PasswordHasher<Account>().VerifyHashedPassword(account, account.Password!, model.passwordHasher!) == PasswordVerificationResult.Failed)
            {
                return null!;
            }
            var response = new TokenResponseDto
            {
                AccessToken = CreateToken(account),
                RefreshToken = await GenerateAndSaveRefreshToken(account)
            };
            return response;
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

        public Task<TokenResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            throw new NotImplementedException();
        }

        public async Task<Account?> RegisterAsync(RegisterDto model)
        {
            if (await _context.GetSystemAccountByEmailAsync(model.Email!) != null)
            {
                return null!;
            }
            Account account = new Account();
            var hashpassword = new PasswordHasher<Account>()
            .HashPassword(account, model.Password!);
            account.Email = model.Email!;
            account.Password = hashpassword;
            account.Role = "U";
            account.Username = model.FullName!;
            await _context.AddAsync(account);
            await _context.SaveChangesAsync();
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
        public void setTokensInsideCookie(TokenResponseDto tokenDto, HttpContext context)
        {
            context.Response.Cookies.Append("accessToken", tokenDto.AccessToken!, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddMinutes(5),
                HttpOnly = true,
                IsEssential = true,
                Secure = true,
                SameSite = SameSiteMode.None
            });
            context.Response.Cookies.Append("refreshToken", tokenDto.AccessToken!, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                HttpOnly = true,
                IsEssential = true,
                Secure = true,
                SameSite = SameSiteMode.None
            });
        }
        public async Task<TokenResponseDto> GenerateTokenPair(Account user)
        {
            var accessToken = _tokenGenerator.GenerateToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.UpdateAsync(user);

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

    }

}
