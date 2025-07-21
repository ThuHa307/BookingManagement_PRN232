using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Domains;
using BusinessObjects.Dtos.Auth;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using WebAPI.Services;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IAccountService _accountService;
        public AuthController(IAuthService authService, IAccountService systemAccountService)
        {
            _authService = authService;
            _accountService = systemAccountService;
        }
        [HttpPost("register")]
        public async Task<ActionResult<Account>> Register(RegisterDto model)
        {
            var account = await _authService.RegisterAsync(model);
            if (account != null)
            {
                return Ok(account);
            }
            return BadRequest("Email already exists.");
        }
        [HttpPost("login")]
        public async Task<ActionResult<TokenResponseDto>> Login(LoginDto model)
        {
            var result = await _authService.LoginAsync(model);
            if (result == null)
            {
                return BadRequest("Invalid username or password");
            }
            _authService.setTokensInsideCookie(result, HttpContext);
            Response.Cookies.Append("accessToken", result.AccessToken!, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddHours(1)
            });
            return Ok(result);
        }
        [HttpPost("refresh-token")]
        public async Task<ActionResult<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto request)
        {
            var result = await _authService.RefreshTokenAsync(request);
            if (result is null || result.AccessToken is null || result.RefreshToken is null)
            {
                return Unauthorized("Invalid refresh token. ");
            }
            return Ok(result);
        }

    }
}