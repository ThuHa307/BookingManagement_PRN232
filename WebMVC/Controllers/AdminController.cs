using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebMVC.API;
using BusinessObjects.Dtos.Account;
using BusinessObjects.Dtos.Common;
using WebMVC.Models;
using Services.Interfaces;

namespace WebMVC.Controllers
{ 
    public class AdminController : Controller
    {
        private readonly AccountApiService _apiService;
        private readonly ILogger<AdminController> _logger;
        private readonly PostApiService _postApiService;

        public AdminController(AccountApiService apiService, ILogger<AdminController> logger, PostApiService postApiService)
        {
            _apiService = apiService;
            _logger = logger;
            _postApiService = postApiService;
        }

        // Dashboard Home
        /*[HttpGet("Index")]
        public async Task<IActionResult> Index(int pageNumber = 1)
        {
            try
            {
                var accessToken = HttpContext.Session.GetString("AccessToken");
                if (string.IsNullOrEmpty(accessToken))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }

                var filter = new AccountFilterDto { PageNumber = pageNumber, PageSize = 10 };
                var accounts = await _apiService.GetAccountsAsync(accessToken, filter);
                var statistics = await _apiService.GetAccountStatisticsAsync(accessToken);

                if (TempData["AlertMessage"] != null)
                {
                    ViewBag.AlertMessage = TempData["AlertMessage"];
                    ViewBag.AlertType = TempData["AlertType"];
                }

                return View("Index", (Accounts: accounts, Statistics: statistics));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                TempData["AlertMessage"] = "Có lỗi xảy ra khi tải dashboard.";
                TempData["AlertType"] = "alert-danger";
                return View("Index", (Accounts: new PagedResultDto<AccountResponseDto>(), Statistics: new { }));
            }
        }*/
        [HttpGet]
        public async Task<IActionResult> Index(int pageNumber = 1)
        {
            try
            {
                var accessToken = HttpContext.Session.GetString("AccessToken");
                if (string.IsNullOrEmpty(accessToken))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }
                var filter = new AccountFilterDto { PageNumber = pageNumber, PageSize = 10 };
                var accounts = await _apiService.GetAccountsAsync(accessToken, filter);
                var statistics = await _apiService.GetAccountStatisticsAsync(accessToken);
               

                var model = new DashboardViewModel
                {
                    Accounts = accounts ?? new BusinessObjects.Dtos.Common.PagedResultDto<BusinessObjects.Dtos.Account.AccountResponseDto>(),
                    Statistics = statistics ?? new AccountStatistics
                    {
                        RoleDistribution = new[] { new RoleDistributionItem { Role = "Unknown", Count = 0 } },
                        AuthProviderDistribution = new[] { new AuthProviderDistributionItem { Provider = "Unknown", Count = 0 } },
                        TotalAccounts = 0,
                        ActiveAccounts = 0,
                        InactiveAccounts = 0,
                        OnlineAccounts = 0,
                        TwoFactorEnabledAccounts = 0
                    }
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải dashboard");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải dashboard.";
                return View(new DashboardViewModel
                {
                    Accounts = new BusinessObjects.Dtos.Common.PagedResultDto<BusinessObjects.Dtos.Account.AccountResponseDto>(),
                    Statistics = new AccountStatistics
                    {
                        RoleDistribution = new[] { new RoleDistributionItem { Role = "Unknown", Count = 0 } },
                        AuthProviderDistribution = new[] { new AuthProviderDistributionItem { Provider = "Unknown", Count = 0 } },
                        TotalAccounts = 0,
                        ActiveAccounts = 0,
                        InactiveAccounts = 0,
                        OnlineAccounts = 0,
                        TwoFactorEnabledAccounts = 0
                    }
                });
            }
        }

        // Account Management
        [HttpGet("Accounts")]
        public async Task<IActionResult> Accounts(AccountFilterDto filter)
        {
            try
            {
                var accessToken = HttpContext.Session.GetString("AccessToken");
                if (string.IsNullOrEmpty(accessToken))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }

                if (filter == null)
                    filter = new AccountFilterDto { PageNumber = 1, PageSize = 10 };

                var result = await _apiService.GetAccountsAsync(accessToken, filter);
                ViewBag.Filter = filter;
                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accounts");
                TempData["AlertMessage"] = "Có lỗi xảy ra khi lấy danh sách tài khoản.";
                TempData["AlertType"] = "alert-danger";
                return View(new PagedResultDto<AccountResponseDto>());
            }
        }

        // Account Detail
        [HttpGet("AccountDetail/{id}")]
        public async Task<IActionResult> AccountDetail(int id)
        {
            try
            {
                var accessToken = HttpContext.Session.GetString("AccessToken");
                if (string.IsNullOrEmpty(accessToken))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }

                var account = await _apiService.GetAccountDetailAsync(accessToken, id);
                if (account == null)
                {
                    TempData["AlertMessage"] = "Không tìm thấy tài khoản.";
                    TempData["AlertType"] = "alert-warning";
                    return RedirectToAction("Accounts");
                }

                return View(account);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account {AccountId}", id);
                TempData["AlertMessage"] = "Có lỗi xảy ra khi lấy thông tin tài khoản.";
                TempData["AlertType"] = "alert-danger";
                return RedirectToAction("Accounts");
            }
        }

        // Create Account - GET
        [HttpGet("CreateAccount")]
        public IActionResult CreateAccount()
        {
            var model = new AccountCreateDto
            {
                IsActive = "A",
                AuthProvider = "local"
            };
            return View(model);
        }

        // Create Account - POST
        [HttpPost("CreateAccount")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAccount(AccountCreateDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var accessToken = HttpContext.Session.GetString("AccessToken");
                if (string.IsNullOrEmpty(accessToken))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }

                var (success, message, account) = await _apiService.CreateAccountAsync(accessToken, model);
                if (!success)
                {
                    TempData["AlertMessage"] = message;
                    TempData["AlertType"] = "alert-danger";
                    return View(model);
                }

                _logger.LogInformation("Admin {AdminId} created account {AccountId}",
                    GetCurrentUserId(), account!.AccountId);

                TempData["AlertMessage"] = "Tạo tài khoản thành công!";
                TempData["AlertType"] = "alert-success";
                return RedirectToAction("AccountDetail", new { id = account.AccountId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating account");
                TempData["AlertMessage"] = "Có lỗi xảy ra khi tạo tài khoản.";
                TempData["AlertType"] = "alert-danger";
                return View(model);
            }
        }

        // Edit Account - GET
        [HttpGet("EditAccount/{id}")]
        public async Task<IActionResult> EditAccount(int id)
        {
            try
            {
                var accessToken = HttpContext.Session.GetString("AccessToken");
                if (string.IsNullOrEmpty(accessToken))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }

                var account = await _apiService.GetAccountDetailAsync(accessToken, id);
                if (account == null)
                {
                    TempData["AlertMessage"] = "Không tìm thấy tài khoản.";
                    TempData["AlertType"] = "alert-warning";
                    return RedirectToAction("Accounts");
                }

                var model = new AccountUpdateDto
                {
                    Email = account.Email,
                    Username = account.Username,
                    Role = account.Role,
                    IsActive = account.IsActive
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account for edit {AccountId}", id);
                TempData["AlertMessage"] = "Có lỗi xảy ra khi lấy thông tin tài khoản.";
                TempData["AlertType"] = "alert-danger";
                return RedirectToAction("Accounts");
            }
        }

        // Edit Account - POST
        [HttpPost("EditAccount/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAccount(int id, AccountUpdateDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var accessToken = HttpContext.Session.GetString("AccessToken");
                if (string.IsNullOrEmpty(accessToken))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }

                var (success, message) = await _apiService.UpdateAccountAsync(accessToken, id, model);
                if (!success)
                {
                    TempData["AlertMessage"] = message;
                    TempData["AlertType"] = "alert-danger";
                    return View(model);
                }

                _logger.LogInformation("Admin {AdminId} updated account {AccountId}",
                    GetCurrentUserId(), id);

                TempData["AlertMessage"] = "Cập nhật tài khoản thành công!";
                TempData["AlertType"] = "alert-success";
                return RedirectToAction("AccountDetail", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating account {AccountId}", id);
                TempData["AlertMessage"] = "Có lỗi xảy ra khi cập nhật tài khoản.";
                TempData["AlertType"] = "alert-danger";
                return View(model);
            }
        }

        // GET: /Admin/DeleteAccount/{id}
        [HttpGet]
        [Route("Admin/DeleteAccount/{id}")]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            try
            {
                var accessToken = HttpContext.Session.GetString("AccessToken");
                if (string.IsNullOrEmpty(accessToken))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }

                var account = await _apiService.GetAccountDetailAsync(accessToken, id);
                if (account == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy tài khoản.";
                    return RedirectToAction("Accounts");
                }

                var currentUserId = GetCurrentUserId();
                if (currentUserId == id)
                {
                    TempData["ErrorMessage"] = "Bạn không thể xóa tài khoản của chính mình.";
                    return RedirectToAction("Accounts");
                }

                return View("DeleteAccount", account);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete account page for AccountId: {AccountId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang xóa tài khoản.";
                return RedirectToAction("Accounts");
            }
        }

        // POST: /Admin/DeleteAccountConfirmed
        [HttpPost]
        [Route("Admin/DeleteAccountConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccountConfirmed([FromForm] int AccountId)
        {
            try
            {
                var accessToken = HttpContext.Session.GetString("AccessToken");
                if (string.IsNullOrEmpty(accessToken))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }

                var currentUserId = GetCurrentUserId();
                if (currentUserId == AccountId)
                {
                    TempData["ErrorMessage"] = "Bạn không thể xóa tài khoản của chính mình.";
                    return RedirectToAction("Accounts");
                }

                var (success, message) = await _apiService.DeleteAccountAsync(accessToken, AccountId);

                _logger.LogInformation("Delete attempt for account {AccountId} by admin {AdminId}. Success: {Success}, Message: {Message}",
                    AccountId, currentUserId, success, message);

                if (!success)
                {
                    TempData["ErrorMessage"] = message;
                    return RedirectToAction("DeleteAccount", new { id = AccountId });
                }

                _logger.LogInformation("Admin {AdminId} successfully deleted account {AccountId}", currentUserId, AccountId);

                TempData["SuccessMessage"] = "Xóa tài khoản thành công!";
                return RedirectToAction("Accounts");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting account {AccountId}", AccountId);
                TempData["ErrorMessage"] = $"Có lỗi xảy ra khi xóa tài khoản: {ex.Message}";
                return RedirectToAction("DeleteAccount", new { id = AccountId });
            }
        }

        // GET: /Admin/Toggle/{id}
        [HttpGet]
        [Route("Admin/Toggle/{id}")]
        public async Task<IActionResult> Toggle(int id)
        {
            try
            {
                var accessToken = HttpContext.Session.GetString("AccessToken");
                if (string.IsNullOrEmpty(accessToken))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }

                var account = await _apiService.GetAccountDetailAsync(accessToken, id);
                if (account == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy tài khoản.";
                    return RedirectToAction("Accounts");
                }

                var currentUserId = GetCurrentUserId();
                if (currentUserId == id)
                {
                    TempData["ErrorMessage"] = "Bạn không thể thay đổi trạng thái tài khoản của chính mình.";
                    return RedirectToAction("Accounts");
                }

                return View("Toggle", account);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading toggle account status page for AccountId: {AccountId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang chuyển đổi trạng thái.";
                return RedirectToAction("Accounts");
            }
        }

        // POST: /Admin/ToggleAccountStatus
        [HttpPost]
        [Route("Admin/ToggleAccountStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAccountStatus([FromForm] int AccountId)
        {
            try
            {
                var accessToken = HttpContext.Session.GetString("AccessToken");
                if (string.IsNullOrEmpty(accessToken))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }

                var account = await _apiService.GetAccountDetailAsync(accessToken, AccountId);
                if (account == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy tài khoản.";
                    return RedirectToAction("Accounts");
                }

                var currentUserId = GetCurrentUserId();
                if (currentUserId == AccountId)
                {
                    TempData["ErrorMessage"] = "Bạn không thể thay đổi trạng thái tài khoản của chính mình.";
                    return RedirectToAction("Accounts");
                }

                var (success, message) = await _apiService.ToggleActiveAsync(accessToken, AccountId);
                if (!success)
                {
                    TempData["ErrorMessage"] = message;
                    return RedirectToAction("Toggle", new { id = AccountId });
                }

                var newStatus = account.IsActive == "A" ? "D" : "A";
                var statusText = newStatus == "A" ? "Kích hoạt" : "Vô hiệu hóa";
                _logger.LogInformation("Admin {AdminId} toggled account {AccountId} status to {Status}",
                    currentUserId, AccountId, statusText);

                TempData["SuccessMessage"] = $"Đã {statusText.ToLower()} tài khoản thành công!";
                return RedirectToAction("Accounts");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling account status {AccountId}", AccountId);
                TempData["ErrorMessage"] = $"Có lỗi xảy ra khi thay đổi trạng thái tài khoản: {ex.Message}";
                return RedirectToAction("Toggle", new { id = AccountId });
            }
        }


        [HttpGet("Statistics")]
        public async Task<IActionResult> Statistics()
        {
            try
            {
                var accessToken = HttpContext.Session.GetString("AccessToken");
                if (string.IsNullOrEmpty(accessToken))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }

                var statistics = await _apiService.GetAccountStatisticsAsync(accessToken);
                if (statistics == null)
                {
                    TempData["ErrorMessage"] = "Không thể tải dữ liệu thống kê.";
                    statistics = new AccountStatistics
                    {
                        RoleDistribution = new[] { new RoleDistributionItem { Role = "Unknown", Count = 0 } },
                        AuthProviderDistribution = new[] { new AuthProviderDistributionItem { Provider = "Unknown", Count = 0 } },
                        TotalAccounts = 0,
                        ActiveAccounts = 0,
                        InactiveAccounts = 0,
                        OnlineAccounts = 0,
                        TwoFactorEnabledAccounts = 0
                    };
                }

                return View(statistics); // Truyền Model trực tiếp thay vì ViewBag
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải thống kê tài khoản");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thống kê.";
                return View(new AccountStatistics
                {
                    RoleDistribution = new[] { new RoleDistributionItem { Role = "Unknown", Count = 0 } },
                    AuthProviderDistribution = new[] { new AuthProviderDistributionItem { Provider = "Unknown", Count = 0 } },
                    TotalAccounts = 0,
                    ActiveAccounts = 0,
                    InactiveAccounts = 0,
                    OnlineAccounts = 0,
                    TwoFactorEnabledAccounts = 0
                });
            }
        }

        

        // Method Posts() cuối cùng trong AdminController:

        [HttpGet("AdminPosts")]
        public async Task<IActionResult> Posts()
        {
            try
            {
                var accessToken = HttpContext.Session.GetString("AccessToken");
                if (string.IsNullOrEmpty(accessToken))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }

                var posts = await _postApiService.GetAllPostsWithAccommodation();

                var viewModel = posts.Select(p => new AdminPostViewModel
                {
                    PostId = p.PostId,
                    Title = string.IsNullOrEmpty(p.Title) ? "Không có tiêu đề" : p.Title,
                    Content = string.IsNullOrEmpty(p.Content) ? "Không có nội dung" : p.Content,
                    CreatorName = string.IsNullOrEmpty(p.CreatorName) ? "Phạm Thanh Vũ" : p.CreatorName,
                    Status = string.IsNullOrEmpty(p.Status) ? "P" : p.Status,
                    CreatedAt = p.CreatedAt
                }).ToList();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách bài viết");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách bài viết.";
                return View(new List<AdminPostViewModel>());
            }
        }

        // Cập nhật PostDetail method để xử lý trường hợp post không tồn tại:
        [HttpGet("PostDetail/{postId:int}")]
        public async Task<IActionResult> PostDetail(int postId)
        {
            try
            {
                var accessToken = HttpContext.Session.GetString("AccessToken");
                if (string.IsNullOrEmpty(accessToken))
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }

                var post = await _postApiService.GetPostDetail(postId);
                if (post == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy bài viết.";
                    return RedirectToAction("Posts");
                }

                var comments = await _postApiService.GetCommentsForPost(postId);
                var commentContents = comments
                    .Where(c => !string.IsNullOrWhiteSpace(c.Content))
                    .Select(c => c.Content)
                    .ToList();

                double? averageScore = null;
                if (commentContents.Any())
                {
                    try
                    {
                        averageScore = await _postApiService.ScoreAverageCommentAsync(
                            post.PostContent ?? "Không có nội dung",
                            commentContents
                        );
                    }
                    catch (Exception scoreEx)
                    {
                        _logger.LogWarning(scoreEx, "Không thể tính điểm cho bài viết {PostId}", postId);
                        // Không throw exception, chỉ để averageScore = null
                    }
                }

                var viewModel = new AdminPostDetailViewModel
                {
                    PostId = post.PostId,
                    Title = string.IsNullOrEmpty(post.Title) ? "Không có tiêu đề" : post.Title,
                    Content = string.IsNullOrEmpty(post.PostContent) ? "Không có nội dung" : post.PostContent,
                    CreatorName = GetCreatorName(post.AccountName),
                    Status = post.CurrentStatus ?? "P",
                    CreatedAt = post.CreatedAt ?? DateTime.Now,
                    AverageScore = averageScore,
                    CommentsCount = comments.Count,
                    Comments = comments.Select(c => new CommentViewModel
                    {
                        CommentId = c.CommentId,
                        Content = c.Content ?? "Không có nội dung",
                        CreatedAt = (DateTime)c.CreatedAt
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải chi tiết bài viết {PostId}", postId);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải chi tiết bài viết.";
                return RedirectToAction("Posts");
            }
        }

        private string GetCreatorName(dynamic account)
        {
            try
            {
                if (account?.UserProfile != null)
                {
                    var firstName = account.UserProfile.FirstName?.ToString() ?? "";
                    var lastName = account.UserProfile.LastName?.ToString() ?? "";
                    var fullName = $"{firstName} {lastName}".Trim();
                    return string.IsNullOrEmpty(fullName) ? "Không xác định" : fullName;
                }
                return "Không xác định";
            }
            catch
            {
                return "Không xác định";
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