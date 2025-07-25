using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebMVC.Controllers
{
    //[Authorize]
    public class ChatRoomController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
} 