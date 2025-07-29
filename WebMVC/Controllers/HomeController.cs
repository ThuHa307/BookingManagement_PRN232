using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebMVC.API;
using WebMVC.Models;

namespace WebMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHomeApiService _homeApiService;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, IHomeApiService homeApiService)
        {
            _logger = logger;
            _configuration = configuration;
            _homeApiService = homeApiService;
        }

        public IActionResult Index()
        {
            if (User.IsInRole("A"))
            {
                return RedirectToAction("Index", "Admin");
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        [Route("/lien-he")]
        public async Task<IActionResult> Contact()
        {
            try
            {
                var contactInfo = await _homeApiService.GetContactInfoAsync();

                ViewData["GoogleMapsAPIKey"] = _configuration["GoogleMapsAPIKey"];
                ViewData["Address"] = contactInfo.Address;
                ViewData["Email"] = contactInfo.Email;
                ViewData["Phone"] = contactInfo.Phone;
                ViewData["Website"] = contactInfo.Website;

                return View(new ContactFormViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải trang liên hệ");
                // Fallback to default values
                ViewData["GoogleMapsAPIKey"] = _configuration["GoogleMapsAPIKey"];
                ViewData["Address"] = "Đại Học FPT, Hòa Hải, Ngũ Hành Sơn, Da Nang 550000, Vietnam";
                ViewData["Email"] = "bluedream.rentnest.company@gmail.com";
                ViewData["Phone"] = "(+84) 941 673 660";
                ViewData["Website"] = "blueHouseDaNang.com";

                return View(new ContactFormViewModel());
            }
        }

        [HttpPost]
        [Route("/lien-he")]
        public async Task<IActionResult> Contact(ContactFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadContactViewData();
                return View(model);
            }

            try
            {
                var result = await _homeApiService.SendContactAsync(model);

                if (result.Success)
                {
                    TempData["Message"] = result.Message;
                    return RedirectToAction("Contact");
                }

                ModelState.AddModelError("", result.Message);
                await LoadContactViewData();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi form liên hệ");
                ModelState.AddModelError("", "Có lỗi xảy ra khi gửi form. Vui lòng thử lại sau.");
                await LoadContactViewData();
                return View(model);
            }
        }

        [Route("/bang-gia-tin")]
        public IActionResult PriceTable()
        {
            return View();
        }

        private async Task LoadContactViewData()
        {
            try
            {
                var contactInfo = await _homeApiService.GetContactInfoAsync();
                ViewData["GoogleMapsAPIKey"] = _configuration["GoogleMapsAPIKey"];
                ViewData["Address"] = contactInfo.Address;
                ViewData["Email"] = contactInfo.Email;
                ViewData["Phone"] = contactInfo.Phone;
                ViewData["Website"] = contactInfo.Website;
            }
            catch
            {
                // Fallback to default values
                ViewData["GoogleMapsAPIKey"] = _configuration["GoogleMapsAPIKey"];
                ViewData["Address"] = "Đại Học FPT, Hòa Hải, Ngũ Hành Sơn, Da Nang 550000, Vietnam";
                ViewData["Email"] = "bluedream.rentnest.company@gmail.com";
                ViewData["Phone"] = "(+84) 941 673 660";
                ViewData["Website"] = "blueHouseDaNang.com";
            }
        }
    }
}
