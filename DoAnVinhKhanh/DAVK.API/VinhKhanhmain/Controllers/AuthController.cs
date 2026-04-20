using FoodGuideAPI.Data;
using FoodGuideAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace FoodGuideAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(AdminUser user)
        {
            var admin = await _context.AdminUsers
                .FirstOrDefaultAsync(x => x.Username == user.Username
                                       && x.Password == user.Password);

            if (admin == null)
            {
                return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu" });
            }

            await WriteAuditLog(admin.Id, "LOGIN", "Auth", $"User {admin.Username} đăng nhập");

            return Ok(new
            {
                message = "Đăng nhập thành công",
                user = admin
            });
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
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new
            {
                message = "Auth API đang hoạt động"
            });
        }
    }
}