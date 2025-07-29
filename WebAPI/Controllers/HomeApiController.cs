using BusinessObjects.Dtos;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using WebMVC.Models;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeApiController : ControllerBase
    {
        private readonly ILogger<HomeApiController> _logger;
        private readonly IMailService _mailService;

        public HomeApiController(ILogger<HomeApiController> logger, IMailService mailService)
        {
            _logger = logger;
            _mailService = mailService;
        }

 
        [HttpPost("contact")]
        public async Task<IActionResult> SendContact([FromBody] ContactFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors });
            }

            try
            {
                var mailToAdmin = new MailContent
                {
                    To = "bluedream.rentnest.company@gmail.com", // Có thể lấy từ configuration
                    Subject = "Liên hệ từ người dùng trên website",
                    Body = $@"
                    <div style='font-family: Arial, sans-serif;'>
                        <h3>Thông tin liên hệ:</h3>
                        <p><strong>Họ tên:</strong> {model.FullName}</p>
                        <p><strong>Email:</strong> {model.Email}</p>
                        <p><strong>Nghề nghiệp:</strong> {model.Occupation}</p>
                        <p><strong>Nội dung:</strong><br/>{model.Message}</p>
                        <br/>
                        <p>Được gửi từ trang liên hệ của website RentNest</p>
                    </div>"
                };

                var mailToCustomer = new MailContent
                {
                    To = model.Email,
                    Subject = "Cảm ơn bạn đã liên hệ với RentNest",
                    Body = $@"
                    <div style='font-family: Arial, sans-serif; color: #333; line-height: 1.6;'>
                        <p>Xin chào <strong>{model.FullName}</strong>,</p>
                        <p>Cảm ơn bạn đã liên hệ với RentNest!</p>
                        <p>Chúng tôi rất vui khi nhận được thông tin từ bạn và sẽ nhanh chóng phản hồi trong thời gian sớm nhất.</p>
                        <p>Nếu bạn cần hỗ trợ gấp hoặc có thắc mắc gì, đừng ngần ngại trả lời email này hoặc gọi cho chúng tôi nhé.</p>
                        <p>Chúc bạn một ngày tuyệt vời!</p>
                        <br/>
                        <p>Trân trọng,<br/><strong>Đội ngũ RentNest</strong></p>
                    </div>"
                };

                var successAdmin = await _mailService.SendMail(mailToAdmin);
                var successCustomer = await _mailService.SendMail(mailToCustomer);

                if (successAdmin && successCustomer)
                {
                    return Ok(new { success = true, message = "Gửi liên hệ thành công! Chúng tôi sẽ phản hồi sớm nhất." });
                }

                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi gửi email. Vui lòng thử lại sau." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email liên hệ");
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi gửi email" });
            }
        }

        [HttpGet("contact-info")]
        public IActionResult GetContactInfo()
        {
            var contactInfo = new
            {
                Address = "Đại Học FPT, Hòa Hải, Ngũ Hành Sơn, Da Nang 550000, Vietnam",
                Email = "bluedream.rentnest.company@gmail.com",
                Phone = "(+84) 941 673 660",
                Website = "blueHouseDaNang.com"
            };

            return Ok(new { success = true, data = contactInfo });
        }
    }
}
