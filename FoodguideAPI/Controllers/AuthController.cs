using FoodGuideAPI.Data;
using FoodGuideAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
                .FirstOrDefaultAsync(x => x.Username == user.Username && x.Password == user.Password);

            if (admin == null)
            {
                return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu" });
            }

            await WriteAuditLog(admin.Id, "LOGIN", "Auth", $"User {admin.Username} đăng nhập");

            return Ok(new
            {
                message = "Đăng nhập thành công",
                accessToken = $"admin-token-{admin.Id}",
                user = new
                {
                    admin.Id,
                    admin.Username,
                    admin.FullName,
                    admin.Role
                }
            });
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me([FromQuery] int userId)
        {
            var admin = await _context.AdminUsers.FindAsync(userId);
            if (admin == null)
            {
                return NotFound(new { message = "Không tìm thấy tài khoản admin" });
            }

            return Ok(admin);
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            if (request.NewPassword != request.ConfirmPassword)
            {
                return BadRequest(new { message = "Mật khẩu xác nhận chưa khớp" });
            }

            var admin = await _context.AdminUsers.FindAsync(request.UserId);
            if (admin == null)
            {
                return NotFound(new { message = "Không tìm thấy tài khoản admin" });
            }

            if (admin.Password != request.CurrentPassword)
            {
                return BadRequest(new { message = "Mật khẩu hiện tại không đúng" });
            }

            admin.Password = request.NewPassword;
            await _context.SaveChangesAsync();

            await WriteAuditLog(admin.Id, "CHANGE_PASSWORD", "Auth", $"User {admin.Username} đổi mật khẩu");

            return Ok(new { message = "Đổi mật khẩu thành công" });
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

        public class ChangePasswordRequest
        {
            public int UserId { get; set; }
            public string CurrentPassword { get; set; } = string.Empty;
            public string NewPassword { get; set; } = string.Empty;
            public string ConfirmPassword { get; set; } = string.Empty;
        }
    }
}
