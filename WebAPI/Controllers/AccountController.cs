using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/v1/accounts")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet("{userId}/profile")] // Endpoint: /api/v1/accounts/{userId}/profile
        public async Task<IActionResult> GetUserProfile(int userId)
        {
            var userProfile = await _accountService.GetAccountByIdAsync(userId); // Giả sử service có method này
            if (userProfile == null)
            {
                return NotFound("User profile not found.");
            }
            return Ok(userProfile);
        }
    }
}