using FoodGuideAPI.Data;
using FoodGuideAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodGuideAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class POIsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public POIsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<POI>>> GetPOIs()
        {
            return await _context.POIs.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<POI>> CreatePOI(POI poi)
        {
            _context.POIs.Add(poi);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPOIs), new { id = poi.Id }, poi);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePOI(int id, POI poi)
        {
            if (id != poi.Id)
            {
                return BadRequest();
            }

            _context.Entry(poi).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePOI(int id)
        {
            var poi = await _context.POIs.FindAsync(id);
            if (poi == null)
            {
                return NotFound();
            }

            _context.POIs.Remove(poi);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<POI>> GetPOI(int id)
        {
            var poi = await _context.POIs.FindAsync(id);
            if (poi == null)
                return NotFound();

            return poi;
        }


        
        [HttpGet("{id}/foods")]
        public async Task<ActionResult<IEnumerable<FoodItem>>> GetFoodsByPOI(int id)
        {
            var foods = await _context.FoodItems
                .Where(f => f.PoiId == id)
                .ToListAsync();

            return foods;
        }
    }
}