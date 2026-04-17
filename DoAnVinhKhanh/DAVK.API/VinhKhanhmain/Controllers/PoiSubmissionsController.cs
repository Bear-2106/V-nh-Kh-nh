using FoodGuideAPI.Data;
using FoodGuideAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace FoodGuideAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PoiSubmissionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PoiSubmissionsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/PoiSubmissions
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _context.PoiSubmissions.ToListAsync());
        }

        // GET: api/PoiSubmissions/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var submission = await _context.PoiSubmissions.FindAsync(id);

            if (submission == null)
                return NotFound(new { message = "Không tìm thấy submission" });

            return Ok(submission);
        }

        // PUT: api/PoiSubmissions/5/approve
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            var submission = await _context.PoiSubmissions.FindAsync(id);

            if (submission == null)
                return NotFound(new { message = "Không tìm thấy submission" });

            if (submission.Status != "Pending")
            {
                return BadRequest(new
                {
                    message = $"Submission này đã được xử lý rồi. Trạng thái hiện tại: {submission.Status}"
                });
            }

            // Đánh dấu đã duyệt trước
            submission.Status = "Approved";
            await _context.SaveChangesAsync();

            // Tạo POI mới từ submission
            var poi = new POI
            {
                Name = submission.PoiName,
                Description = submission.Description,
                Address = submission.Address,
                Latitude = submission.Latitude,
                Longitude = submission.Longitude,
                Radius = submission.Radius,
                TtsTextVi = submission.TtsTextVi,
                TtsTextEn = submission.TtsTextEn,
                TtsTextZh = submission.TtsTextZh,
                TtsTextFr = submission.TtsTextFr,
                TtsTextRu = submission.TtsTextRu,
                ImageUrl = submission.ImageUrl,
                Category = submission.Category,
                IsActive = true
            };

            _context.POIs.Add(poi);
            await _context.SaveChangesAsync();

            await WriteAuditLog(
                null,
                "APPROVE",
                "PoiSubmission",
                $"Duyệt submission '{submission.PoiName}' và tạo POI Id={poi.Id}"
            );

            return Ok(new
            {
                message = "Duyệt submission thành công",
                submission,
                poi
            });
        }

        // PUT: api/PoiSubmissions/5/reject
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> Reject(int id)
        {
            var submission = await _context.PoiSubmissions.FindAsync(id);

            if (submission == null)
                return NotFound(new { message = "Không tìm thấy submission" });

            if (submission.Status != "Pending")
            {
                return BadRequest(new
                {
                    message = $"Submission này đã được xử lý rồi. Trạng thái hiện tại: {submission.Status}"
                });
            }

            submission.Status = "Rejected";
            await _context.SaveChangesAsync();

            await WriteAuditLog(
                null,
                "REJECT",
                "PoiSubmission",
                $"Từ chối submission '{submission.PoiName}'"
            );

            return Ok(new
            {
                message = "Từ chối submission thành công",
                submission
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
        public async Task<IActionResult> Create(PoiSubmission submission)
        {
            submission.Status = "Pending";
            submission.CreatedAt = DateTime.Now;

            _context.PoiSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            return Ok(submission);
        }
    }
}
