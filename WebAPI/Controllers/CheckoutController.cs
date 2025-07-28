using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Net.payOS;
using Net.payOS.Types;
using RentNest.Core.Model.PayOS;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/payos/[controller]")]
    public class CheckoutController : ControllerBase
    {
        private readonly PayOS _payOS;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(PayOS payOS, IHttpContextAccessor httpContextAccessor, ILogger<CheckoutController> logger)
        {
            _payOS = payOS;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }
        [HttpPost("create")]
        public async Task<ActionResult<PayOSResponseModel>> CreatePaymentLink([FromBody] CreatePaymentLinkRequest request)
        {
            try
            {
                int orderCode = int.Parse(DateTimeOffset.Now.ToString("ffffff"));

                var items = new List<ItemData>
                {
                    new ItemData(request.productName, 1, request.price)
                };

                var baseUrl = $"{Request.Scheme}://{Request.Host}";

                var paymentData = new PaymentData(
                    orderCode,
                    request.price,
                    request.description,
                    items,
                    request.cancelUrl,
                    request.returnUrl
                );

                var createPayment = await _payOS.createPaymentLink(paymentData);

                _logger.LogInformation("Created PayOS link: {CheckoutUrl}, OrderCode: {OrderCode}",
                    createPayment.checkoutUrl, orderCode);

                return new PayOSResponseModel
                {
                    Amount = request.price,
                    IsSuccess = true,
                    CreatedAt = DateTime.Now,
                    CheckoutUrl = createPayment.checkoutUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create PayOS link");
                return StatusCode(500, "Create payment link failed");
            }
        }

    }
}