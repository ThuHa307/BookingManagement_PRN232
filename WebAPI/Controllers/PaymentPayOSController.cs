using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Domains;
using DataAccessObjects;
using Microsoft.AspNetCore.Mvc;
using RentNest.Core.Enums;
using RentNest.Core.Model.PayOS;
using RentNest.Core.UtilHelper;
using RentNest.Infrastructure.DataAccess;


namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentPayOSController : ControllerBase
    {
        private readonly ILogger<PaymentPayOSController> _logger;
        private readonly PostDAO _postDAO;
        private readonly PostPackageDetailDAO _postPackageDetailDAO;
        private readonly PaymentDAO _paymentDAO;

        public PaymentPayOSController(
                ILogger<PaymentPayOSController> logger,
                PostDAO postDAO,
                PostPackageDetailDAO postPackageDetailDAO,
                PaymentDAO paymentDAO)
        {
            _logger = logger;
            _postDAO = postDAO;
            _postPackageDetailDAO = postPackageDetailDAO;
            _paymentDAO = paymentDAO;
        }
        [HttpGet("success")]
        public async Task<IActionResult> PaymentSuccess(
          [FromQuery] int amount,
          [FromQuery] string transactionId,
          [FromQuery] int postId)
        {
            try
            {
                var post = await _postDAO.GetByIdAsync(postId);
                post.CurrentStatus = "A";
                await _postDAO.UpdateAsync(post);
                if (post == null) return NotFound("Post not found");

                var postPackage = await _postPackageDetailDAO.GetByPostIdAsync(postId);
                if (postPackage == null) return NotFound("Post package not found");

                string packageTypeName = postPackage.Pricing?.PackageType.PackageTypeName ?? "Tin thường";
                var packageType = BadgeHelper.ParsePackageType(packageTypeName);

                post.CurrentStatus = BadgeHelper.IsVip(packageType)
                    ? PostStatusHelper.ToDbValue(PostStatus.Active)
                    : PostStatusHelper.ToDbValue(PostStatus.Active);

                postPackage.PaymentStatus = PostPackagePaymentStatusHelper.ToDbValue(PostPackagePaymentStatus.Completed);

                postPackage.PaymentTransactionId = transactionId;

                var payment = new Payment
                {
                    PostPackageDetailsId = postPackage.Id,
                    TotalPrice = postPackage.TotalPrice,
                    Status = PaymentStatusHelper.ToDbValue(PaymentStatus.Success),
                    PaymentDate = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    MethodId = 2,
                    AccountId = post.AccountId
                };

                await _paymentDAO.AddAsync(payment);
                await _postDAO.UpdateAsync(post);
                await _postPackageDetailDAO.UpdateAsync(postPackage);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment success failed for postId {PostId}", postId);
                return StatusCode(500, new { success = false, message = "Payment error" });
            }
        }
        [HttpGet("cancel")]
        public async Task<IActionResult> Cancel([FromQuery] int postId, [FromQuery] int amount = 0)
        {
            try
            {
                if (postId <= 0) return BadRequest("Invalid postId");

                var post = await _postDAO.GetByIdAsync(postId);
                if (post == null) return NotFound("Post not found");

                var postPackage = await _postPackageDetailDAO.GetByPostIdAsync(postId);
                if (postPackage == null) return NotFound("Post package not found");

                post.CurrentStatus = PostStatusHelper.ToDbValue(PostStatus.Cancelled);
                postPackage.PaymentStatus = PostPackagePaymentStatusHelper.ToDbValue(PostPackagePaymentStatus.Inactive);

                await _postDAO.UpdateAsync(post);
                await _postPackageDetailDAO.UpdateAsync(postPackage);

                return Ok(new PayOSResponseModel
                {
                    Amount = amount > 0 ? amount : 50000,
                    IsSuccess = false,
                    CreatedAt = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cancel payment failed for postId {PostId}", postId);
                return StatusCode(500, new { success = false });
            }
        }

    }
}