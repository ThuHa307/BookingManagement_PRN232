using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using WebMVC.API;
using RentNest.Core.Model.PayOS;

namespace WebMVC.Controllers
{
    [Route("[controller]")]
    public class PaymentPayOSController : Controller
    {
        private readonly PaymentPayOSApiService _paymentService;
        private readonly ILogger<PaymentPayOSController> _logger;

        public PaymentPayOSController(PaymentPayOSApiService paymentService, ILogger<PaymentPayOSController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        // PayOS success callback
        [HttpGet("success")]
        public async Task<IActionResult> PaymentSuccess(
            [FromQuery] int amount,
            [FromQuery(Name = "id")] string transactionId, // map từ id -> transactionId
            [FromQuery] int postId,
            [FromQuery] string code = null,
            [FromQuery] string status = null)
        {
            _logger.LogInformation("PayOS success callback: postId={PostId}, amount={Amount}, txn={Txn}, code={Code}, status={Status}",
                postId, amount, transactionId, code, status);

            if (status?.ToUpper() != "PAID" || code != "00")
            {
                return Redirect("/?error=payment_failed");
            }

            var success = await _paymentService.HandlePaymentSuccessAsync(amount, transactionId, postId);
            if (!success)
                return Redirect("/?error=payment_error");

            // Tạo model trả về view
            var model = new PayOSResponseModel
            {
                Amount = amount,
                CreatedAt = DateTime.Now
            };

            return View("Success", model);
        }

        // PayOS cancel callback
        [HttpGet("cancel")]
        public async Task<IActionResult> Cancel([FromQuery] int postId, [FromQuery] int amount = 0)
        {
            _logger.LogInformation("PayOS cancel callback: postId={PostId}, amount={Amount}", postId, amount);

            var model = await _paymentService.HandlePaymentCancelAsync(postId, amount);
            if (model == null)
                return Redirect("/?error=cancel_error");

            // Hiển thị Cancel.cshtml với model
            return View("Cancel", model);
        }
    }
}
