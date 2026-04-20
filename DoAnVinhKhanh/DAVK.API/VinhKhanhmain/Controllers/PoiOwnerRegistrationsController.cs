using FoodGuideAPI.Data;
using FoodGuideAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace FoodGuideAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PoiOwnerRegistrationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PoiOwnerRegistrationsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/PoiOwnerRegistrations
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _context.PoiOwnerRegistrations.ToListAsync());
        }

        // GET: api/PoiOwnerRegistrations/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.PoiOwnerRegistrations.FindAsync(id);

            if (item == null)
                return NotFound(new { message = "Không tìm thấy đơn đăng ký" });

            return Ok(item);
        }

        // PUT: api/PoiOwnerRegistrations/5/approve
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            var item = await _context.PoiOwnerRegistrations.FindAsync(id);

            if (item == null)
                return NotFound(new { message = "Không tìm thấy đơn đăng ký" });

            if (item.Status != "Pending")
            {
                return BadRequest(new
                {
                    message = $"Đơn đăng ký này đã được xử lý rồi. Trạng thái hiện tại: {item.Status}"
                });
            }

            item.Status = "Approved";
            await _context.SaveChangesAsync();

            await WriteAuditLog(
                null,
                "APPROVE",
                "OwnerRegistration",
                $"Duyệt owner '{item.FullName}'"
            );

            return Ok(new
            {
                message = "Duyệt đơn đăng ký thành công",
                registration = item
            });
        }

        // PUT: api/PoiOwnerRegistrations/5/reject
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> Reject(int id)
        {
            var item = await _context.PoiOwnerRegistrations.FindAsync(id);

            if (item == null)
                return NotFound(new { message = "Không tìm thấy đơn đăng ký" });

            if (item.Status != "Pending")
            {
                return BadRequest(new
                {
                    message = $"Đơn đăng ký này đã được xử lý rồi. Trạng thái hiện tại: {item.Status}"
                });
            }

            item.Status = "Rejected";
            await _context.SaveChangesAsync();

            await WriteAuditLog(
                null,
                "REJECT",
                "OwnerRegistration",
                $"Từ chối owner '{item.FullName}'"
            );

            return Ok(new
            {
                message = "Từ chối đơn đăng ký thành công",
                registration = item
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
        [HttpPost]
        public async Task<IActionResult> Create(PoiOwnerRegistration item)
        {
            item.Status = "Pending";
            item.CreatedAt = DateTime.Now;

            _context.PoiOwnerRegistrations.Add(item);
            await _context.SaveChangesAsync();

            return Ok(item);
        }
    }
}