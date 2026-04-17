using FoodGuideAPI.Data;
using FoodGuideAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodGuideAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PoiLocalizationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PoiLocalizationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _context.PoiLocalizations.ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.PoiLocalizations.FindAsync(id);

            if (item == null)
                return NotFound(new { message = "Không tìm thấy bản dịch" });

            return Ok(item);
        }

        [HttpGet("poi/{poiId}")]
        public async Task<IActionResult> GetByPoi(int poiId)
        {
            var data = await _context.PoiLocalizations
                .Where(x => x.PoiId == poiId)
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("poi/{poiId}/lang/{lang}")]
        public async Task<IActionResult> GetByPoiAndLang(int poiId, string lang)
        {
            var data = await _context.PoiLocalizations
                .FirstOrDefaultAsync(x => x.PoiId == poiId && x.Language == lang);

            if (data == null)
                return NotFound(new { message = "Không tìm thấy bản dịch theo ngôn ngữ" });

            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Create(PoiLocalization item)
        {
            _context.PoiLocalizations.Add(item);
            await _context.SaveChangesAsync();

            return Ok(item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, PoiLocalization item)
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
            var item = await _context.PoiLocalizations.FindAsync(id);

            if (item == null)
                return NotFound(new { message = "Không tìm thấy bản dịch" });

            _context.PoiLocalizations.Remove(item);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}