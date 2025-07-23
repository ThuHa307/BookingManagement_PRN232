using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/v1/accommodation-types")] // <--- Route này phải khớp với cuộc gọi từ MVC
    public class AccommodationTypesController : ControllerBase
    {
        private readonly IAccommodationTypeService _accommodationTypeService;

        public AccommodationTypesController(IAccommodationTypeService accommodationTypeService)
        {
            _accommodationTypeService = accommodationTypeService;
        }

        [HttpGet] // <--- GET method để lấy danh sách
        public async Task<IActionResult> GetAccommodationTypes()
        {
            var result = await _accommodationTypeService.GetAllAsync(); // Gọi phương thức Service
            if (result == null) // Xử lý trường hợp không tìm thấy dữ liệu
            {
                return NotFound("No accommodation types found.");
            }
            return Ok(result); // Trả về 200 OK với dữ liệu
        }
    }
}