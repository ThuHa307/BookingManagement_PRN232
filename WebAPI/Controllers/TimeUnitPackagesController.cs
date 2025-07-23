using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/v1/time-unit-packages")] // <--- Route này phải khớp
    public class TimeUnitPackagesController : ControllerBase
    {
        private readonly ITimeUnitPackageService _timeUnitPackageService;

        public TimeUnitPackagesController(ITimeUnitPackageService timeUnitPackageService)
        {
            _timeUnitPackageService = timeUnitPackageService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTimeUnitPackages()
        {
            var result = await _timeUnitPackageService.GetAll(); // Gọi phương thức Service
            if (result == null)
            {
                return NotFound("No time unit packages found.");
            }
            return Ok(result);
        }
    }
}