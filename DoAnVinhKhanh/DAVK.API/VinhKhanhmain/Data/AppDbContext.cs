using FoodGuideAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodGuideAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<POI> POIs { get; set; }
        public DbSet<FoodItem> FoodItems { get; set; }
        public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<PoiOwnerRegistration> PoiOwnerRegistrations {  get; set; }  
        public DbSet<PoiSubmission> PoiSubmissions { get; set; }
        public DbSet<PoiLocalization> PoiLocalizations { get; set; }
        public DbSet<AiUsageLimit> AiUsageLimits { get; set; }
    }
}