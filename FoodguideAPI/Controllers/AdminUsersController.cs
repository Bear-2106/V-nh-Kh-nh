using FoodGuideAPI.Data;
using FoodGuideAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace FoodGuideAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminUsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminUsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AdminUser>>> GetAdminUsers()
        {
            return await _context.AdminUsers.ToListAsync();
        }
    
        [HttpGet("{id}")]
        public async Task<ActionResult<AdminUser>> GetAdminUser(int id)
        {
            var user = await _context.AdminUsers.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        [HttpPost]
        public async Task<ActionResult<AdminUser>> CreateAdminUser(AdminUser user)
        {
            _context.AdminUsers.Add(user);
            await _context.SaveChangesAsync();

            await WriteAuditLog(null, "CREATE", "AdminUser", $"Tạo user: {user.Username}");

            return CreatedAtAction(nameof(GetAdminUser), new { id = user.Id }, user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAdminUser(int id, AdminUser user)
        {
            if (id != user.Id)
            {
                return BadRequest();
            }

            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            await WriteAuditLog(null, "UPDATE", "AdminUser", $"Sửa user: {user.Username}");

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAdminUser(int id)
        {
            var user = await _context.AdminUsers.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            _context.AdminUsers.Remove(user);
            await _context.SaveChangesAsync();

            await WriteAuditLog(null, "DELETE", "AdminUser", $"Xóa user: {user.Username}");

            return NoContent();
        }

        private async Task WriteAuditLog(int? userId, string action, string resource, string description)
        {
            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                Resource = resource,
                Description = description,
                CreatedAt = DateTime.Now
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}