using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Net.payOS.Types;
using Net.payOS;
using WebMVC.API;
using RentNest.Core.Model.PayOS;
namespace WebMVC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CheckoutController : Controller
    {
        private readonly PayOSApiService _payOSApiService;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(PayOSApiService payOSApiService, ILogger<CheckoutController> logger)
        {
            _payOSApiService = payOSApiService;
            _logger = logger;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return View("Index");
        }

        [HttpPost("CheckoutPayment")]
        public async Task<IActionResult> CheckoutPayment(
    [FromForm] string ItemName,
    [FromForm] int AmountPayOs,
    [FromForm] int postId) // Nhận thêm postId
        {
            var request = new CreatePaymentLinkRequest(
                ItemName,
                "Gói đăng ký dịch vụ",
                AmountPayOs,
                // returnUrl trỏ về PaymentPayOSController với postId
                $"{Request.Scheme}://{Request.Host}/PaymentPayOS/success?amount={AmountPayOs}&postId={postId}",
                $"{Request.Scheme}://{Request.Host}/PaymentPayOS/cancel?amount={AmountPayOs}&postId={postId}"
            );

            var result = await _payOSApiService.CreatePaymentLinkAsync(request);

            if (result == null || !result.IsSuccess)
            {
                TempData["Error"] = "Tạo thanh toán PayOS thất bại!";
                return RedirectToAction("Index");
            }

            // Chuyển hướng sang trang PayOS
            return Redirect(result.CheckoutUrl);
        }


    }
}