using Microsoft.AspNetCore.Mvc;

namespace FoodGuideAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "ok",
                service = "FoodGuideAPI",
                time = DateTime.Now
            });
        }
    }
}
