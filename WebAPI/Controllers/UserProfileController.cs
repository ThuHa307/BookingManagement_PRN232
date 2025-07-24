using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BusinessObjects.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Yêu cầu đăng nhập cho tất cả endpoints
    public class UserProfileController : ControllerBase
    {
        private readonly IUserProfileService _userProfileService;
        private readonly ILogger<UserProfileController> _logger;

        public UserProfileController(IUserProfileService userProfileService, ILogger<UserProfileController> logger)
        {
            _userProfileService = userProfileService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy thông tin profile của user hiện tại
        /// </summary>
        /// <returns></returns>
        [HttpGet("my-profile")]
        public async Task<ActionResult<UserProfileDto>> GetMyProfile()
        {
            try
            {
                var accountId = GetCurrentAccountId();
                if (accountId == null)
                {
                    return Unauthorized("Thông tin người dùng không hợp lệ.");
                }

                var profile = await _userProfileService.GetUserProfileAsync(accountId.Value);
                
                // Nếu account mới tạo chưa có profile thì trả về thông báo
                if (profile == null)
                {
                    return Ok(new { message = "Profile chưa được tạo", profile = (UserProfileDto?)null });
                }

                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user profile");
                return StatusCode(500, "Có lỗi xảy ra khi lấy thông tin profile");
            }
        }

        /// <summary>
        /// Lấy thông tin profile của user theo account ID (chỉ admin)
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        [HttpGet("{accountId}")]
        [Authorize(Roles = "A")] // Chỉ admin mới có thể xem profile của user khác
        public async Task<ActionResult<UserProfileDto>> GetUserProfile(int accountId)
        {
            try
            {
                var profile = await _userProfileService.GetUserProfileAsync(accountId);
                
                if (profile == null)
                {
                    return NotFound("Không tìm thấy profile hoặc account không tồn tại");
                }

                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile for account {AccountId}", accountId);
                return StatusCode(500, "Có lỗi xảy ra khi lấy thông tin profile");
            }
        }

        /// <summary>
        /// Tạo mới hoặc cập nhật profile của user hiện tại
        /// </summary>
        /// <param name="userProfileDto"></param>
        /// <returns></returns>
        [HttpPost("my-profile")]
        public async Task<ActionResult<UserProfileDto>> CreateOrUpdateMyProfile([FromBody] UserProfileDto userProfileDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var accountId = GetCurrentAccountId();
                if (accountId == null)
                {
                    return Unauthorized("Thông tin người dùng không hợp lệ.");
                }

                var updatedProfile = await _userProfileService.CreateOrUpdateUserProfileAsync(accountId.Value, userProfileDto);
                
                _logger.LogInformation("User profile updated for account {AccountId}", accountId.Value);
                return Ok(updatedProfile);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating user profile");
                return StatusCode(500, "Có lỗi xảy ra khi cập nhật profile");
            }
        }

        /// <summary>
        /// Upload avatar cho user hiện tại
        /// </summary>
        /// <param name="avatar"></param>
        /// <returns></returns>
        [HttpPost("avatar")]
        public async Task<ActionResult<object>> UploadAvatar(IFormFile avatar)
        {
            try
            {
                var accountId = GetCurrentAccountId();
                if (accountId == null)
                {
                    return Unauthorized("Thông tin người dùng không hợp lệ.");
                }

                var avatarUrl = await _userProfileService.UploadAvatarAsync(accountId.Value, avatar);
                
                _logger.LogInformation("Avatar uploaded for account {AccountId}", accountId.Value);
                return Ok(new { avatarUrl = avatarUrl, message = "Upload avatar thành công" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar");
                return StatusCode(500, "Có lỗi xảy ra khi upload avatar");
            }
        }

        /// <summary>
        /// Xóa avatar của user hiện tại
        /// </summary>
        /// <returns></returns>
        [HttpDelete("avatar")]
        public async Task<IActionResult> DeleteAvatar()
        {
            try
            {
                var accountId = GetCurrentAccountId();
                if (accountId == null)
                {
                    return Unauthorized("Thông tin người dùng không hợp lệ.");
                }

                var result = await _userProfileService.DeleteAvatarAsync(accountId.Value);
                
                if (!result)
                {
                    return NotFound("Không tìm thấy avatar để xóa");
                }

                _logger.LogInformation("Avatar deleted for account {AccountId}", accountId.Value);
                return Ok(new { message = "Xóa avatar thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting avatar");
                return StatusCode(500, "Có lỗi xảy ra khi xóa avatar");
            }
        }

        private int? GetCurrentAccountId()
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (accountIdClaim != null && int.TryParse(accountIdClaim.Value, out int accountId))
            {
                return accountId;
            }
            return null;
        }
    }
}