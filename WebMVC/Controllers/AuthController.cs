using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BusinessObjects.Dtos.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebMVC.Controllers
{
    [Route("[controller]")]
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> _logger;

        private readonly HttpClient _httpClient;
        public AuthController(ILogger<AuthController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("http://localhost:5290");
        }
        [HttpGet("Login")]
        public IActionResult Login()
        {
            return View(); // sẽ tìm file Views/Auth/Login.cshtml
        }
        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Đăng nhập thành công! Đang chuyển hướng đến trang chủ...";
                    return RedirectToAction("Index", "Home");
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Đăng nhập thất bại: " + error);

                TempData["ErrorMessage"] = "Email hoặc mật khẩu không đúng. Vui lòng thử lại.";
                return RedirectToAction("Login", "Auth");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi đăng nhập hệ thống");
                TempData["ErrorMessage"] = "Hệ thống đang gặp sự cố. Vui lòng thử lại sau.";
                return RedirectToAction("Login", "Auth");
            }

        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}