using FoodGuideAPI.Data;
using FoodGuideAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodGuideAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AiUsageLimitsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AiUsageLimitsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _context.AiUsageLimits.ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.AiUsageLimits.FindAsync(id);

            if (item == null)
                return NotFound(new { message = "Không tìm thấy dữ liệu AI usage" });

            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create(AiUsageLimit item)
        {
            _context.AiUsageLimits.Add(item);
            await _context.SaveChangesAsync();

            return Ok(item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, AiUsageLimit item)
        {
            if (id != item.Id)
                return BadRequest(new { message = "Id không khớp" });

            _context.Entry(item).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.AiUsageLimits.FindAsync(id);

            if (item == null)
                return NotFound(new { message = "Không tìm thấy dữ liệu AI usage" });

            _context.AiUsageLimits.Remove(item);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}