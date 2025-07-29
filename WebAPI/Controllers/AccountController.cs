using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BusinessObjects.Dtos.Account;
using BusinessObjects.Dtos.Common;
using Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Roles = "A")] // Only Admin can access
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IAccountService accountService,
            ILogger<AccountController> logger)
        {
            _accountService = accountService;
            _logger = logger;
        }

        /// <summary>
        /// Get all accounts with filtering and pagination
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResultDto<AccountResponseDto>>> GetAccounts([FromQuery] AccountFilterDto filter)
        {
            try
            {
                var result = await _accountService.GetAccountsAsync(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accounts");
                return StatusCode(500, "Có lỗi xảy ra khi lấy danh sách tài khoản.");
            }
        }

        /// <summary>
        /// Get account detail by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<AccountResponseDto>> GetAccount(int id)
        {
            try
            {
                var account = await _accountService.GetAccountDetailAsync(id);
                if (account == null)
                {
                    return NotFound("Không tìm thấy tài khoản.");
                }

                return Ok(account);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account {AccountId}", id);
                return StatusCode(500, "Có lỗi xảy ra khi lấy thông tin tài khoản.");
            }
        }

        /// <summary>
        /// Create new account
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<AccountResponseDto>> CreateAccount([FromBody] AccountCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var (success, message, account) = await _accountService.CreateAccountAsync(dto);
                if (!success)
                {
                    return BadRequest(new { Message = message });
                }

                _logger.LogInformation("Admin {AdminId} created account {AccountId}",
                    GetCurrentUserId(), account!.AccountId);

                return CreatedAtAction(nameof(GetAccount), new { id = account.AccountId }, account);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating account");
                return StatusCode(500, "Có lỗi xảy ra khi tạo tài khoản.");
            }
        }

        /// <summary>
        /// Update existing account
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAccount(int id, [FromBody] AccountUpdateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var (success, message) = await _accountService.UpdateAccountAsync(id, dto);
                if (!success)
                {
                    return BadRequest(new { Message = message });
                }

                _logger.LogInformation("Admin {AdminId} updated account {AccountId}",
                    GetCurrentUserId(), id);

                return Ok(new { Message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating account {AccountId}", id);
                return StatusCode(500, "Có lỗi xảy ra khi cập nhật tài khoản.");
            }
        }

        /// <summary>
        /// Delete account (hard delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                // Prevent admin from deleting their own account
                if (currentUserId == id)
                {
                    return BadRequest(new { Message = "Bạn không thể xóa tài khoản của chính mình." });
                }

                var (success, message) = await _accountService.DeleteAccountAsync(id);
                if (!success)
                {
                    return BadRequest(new { Message = message });
                }

                _logger.LogInformation("Admin {AdminId} deleted account {AccountId}",
                    currentUserId, id);

                return Ok(new { Message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting account {AccountId}", id);
                return StatusCode(500, "Có lỗi xảy ra khi xóa tài khoản.");
            }
        }

        /// <summary>
        /// Toggle account active status (A <-> D)
        /// </summary>
        [HttpPatch("{id}/toggle-active")]
        public async Task<IActionResult> ToggleAccountActiveStatus(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                // Prevent admin from toggling their own account status
                if (currentUserId == id)
                {
                    return BadRequest(new { Message = "Bạn không thể thay đổi trạng thái của tài khoản của chính mình." });
                }

                var (success, message) = await _accountService.ToggleAccountActiveStatusAsync(id);
                if (!success)
                {
                    return BadRequest(new { Message = message });
                }

                _logger.LogInformation("Admin {AdminId} toggled active status for account {AccountId}",
                    currentUserId, id);

                return Ok(new { Message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling active status for account {AccountId}", id);
                return StatusCode(500, "Có lỗi xảy ra khi thay đổi trạng thái tài khoản.");
            }
        }

        /// <summary>
        /// Get account statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult> GetAccountStatistics()
        {
            try
            {
                // Get all accounts to calculate statistics
                var allAccounts = await _accountService.GetAccountsAsync(new AccountFilterDto
                {
                    PageSize = int.MaxValue,
                    PageNumber = 1
                });

                var stats = new
                {
                    TotalAccounts = allAccounts.TotalCount,
                    ActiveAccounts = allAccounts.Items.Count(a => a.IsActive == "A"),
                    InactiveAccounts = allAccounts.Items.Count(a => a.IsActive == "D"),
                    OnlineAccounts = allAccounts.Items.Count(a => a.IsOnline == true),
                    TwoFactorEnabledAccounts = allAccounts.Items.Count(a => a.TwoFactorEnabled == true),
                    RoleDistribution = allAccounts.Items.GroupBy(a => a.Role).Select(g => new
                    {
                        Role = g.Key switch
                        {
                            "U" => "User",
                            "A" => "Admin",
                            "S" => "Staff",
                            "L" => "Landlord",
                            _ => "Unknown"
                        },
                        Count = g.Count()
                    }),
                    AuthProviderDistribution = allAccounts.Items.GroupBy(a => a.AuthProvider).Select(g => new
                    {
                        Provider = g.Key switch
                        {
                            "local" => "Local",
                            "google" => "Google",
                            "facebook" => "Facebook",
                            _ => g.Key
                        },
                        Count = g.Count()
                    })
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account statistics");
                return StatusCode(500, "Có lỗi xảy ra khi lấy thống kê tài khoản.");
            }
        }

        #region Private Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) ? userId : 0;
        }


        #endregion
    }
}