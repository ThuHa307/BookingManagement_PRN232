using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/v1/amenities")] // <--- Route này phải khớp
    public class AmenitiesController : ControllerBase
    {
        private readonly IAmenitiesSerivce _amenitiesService;

        public AmenitiesController(IAmenitiesSerivce amenitiesService)
        {
            _amenitiesService = amenitiesService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAmenities()
        {
            var result = await _amenitiesService.GetAll(); // Gọi phương thức Service
            if (result == null)
            {
                return NotFound("No amenities found.");
            }
            return Ok(result);
        }
    }
}