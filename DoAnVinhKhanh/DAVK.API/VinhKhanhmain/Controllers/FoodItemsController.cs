using FoodGuideAPI.Data;
using FoodGuideAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodGuideAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FoodItemsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FoodItemsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FoodItem>>> GetFoodItems()
        {
            return await _context.FoodItems.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<FoodItem>> CreateFoodItem(FoodItem foodItem)
        {
            _context.FoodItems.Add(foodItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFoodItems), new { id = foodItem.Id }, foodItem);
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<FoodItem>> GetFoodItem(int id)
        {
            var item = await _context.FoodItems.FindAsync(id);
            if (item == null)
                return NotFound();

            return item;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFoodItem(int id, FoodItem item)
        {
            if (id != item.Id)
                return BadRequest();

            _context.Entry(item).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFoodItem(int id)
        {
            var item = await _context.FoodItems.FindAsync(id);
            if (item == null)
                return NotFound();

            _context.FoodItems.Remove(item);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}