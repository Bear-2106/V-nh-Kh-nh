using System.ComponentModel.DataAnnotations;

namespace FoodGuideAdmin.Models
{
    public class AdminSessionUser
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
        public string Search { get; set; } = string.Empty;
    }

    public class DashboardViewModel
    {
        public int TotalPois { get; set; }
        public int TotalFoods { get; set; }
        public int TotalAdminUsers { get; set; }
        public int TotalRoles { get; set; }
        public int PendingRegistrations { get; set; }
        public int PendingSubmissions { get; set; }
        public int TotalLocalizations { get; set; }
        public int TotalAiUsageLimits { get; set; }
        public string SelectedCategory { get; set; } = "All";
        public List<string> Categories { get; set; } = new();
        public List<DashboardCategorySummary> CategorySummaries { get; set; } = new();
        public List<DashboardPoiFoodSummary> PoiFoodSummaries { get; set; } = new();
        public List<AuditLogItemViewModel> RecentAuditLogs { get; set; } = new();
    }

    public class DashboardCategorySummary
    {
        public string Category { get; set; } = string.Empty;
        public int PoiCount { get; set; }
        public double Percentage { get; set; }
    }

    public class DashboardPoiFoodSummary
    {
        public int PoiId { get; set; }
        public string PoiName { get; set; } = string.Empty;
        public int FoodCount { get; set; }
    }

    public class PoiItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Radius { get; set; }
        public string AudioUrl { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class PoiEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên POI là bắt buộc")]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Radius { get; set; }
        public string AudioUrl { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class PoiDetailsViewModel
    {
        public PoiItemViewModel Poi { get; set; } = new();
        public List<FoodItemViewModel> Foods { get; set; } = new();
        public List<PoiLocalizationViewModel> Localizations { get; set; } = new();
    }

    public class FoodItemViewModel
    {
        public int Id { get; set; }
        public int PoiId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
    }

    public class FoodEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "POI là bắt buộc")]
        public int PoiId { get; set; }

        [Required(ErrorMessage = "Tên món là bắt buộc")]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsAvailable { get; set; } = true;
    }

    public class OwnerRegistrationViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
        public string BusinessAddress { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class PoiSubmissionViewModel
    {
        public int Id { get; set; }
        public int? OwnerId { get; set; }
        public string PoiName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Radius { get; set; }
        public string AudioUrl { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AdminUserViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class AdminUserEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Username là bắt buộc")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vai trò là bắt buộc")]
        public string Role { get; set; } = string.Empty;
    }

    public class RoleViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Priority { get; set; }
        public string Permissions { get; set; } = string.Empty;
        public bool IsSystem { get; set; }
    }

    public class RoleEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên vai trò là bắt buộc")]
        public string Name { get; set; } = string.Empty;

        public int Priority { get; set; }
        public string Permissions { get; set; } = string.Empty;
        public bool IsSystem { get; set; }
    }

    public class PoiLocalizationViewModel
    {
        public int Id { get; set; }
        public int PoiId { get; set; }
        public string Language { get; set; } = string.Empty;
        public string LocalizedName { get; set; } = string.Empty;
        public string LocalizedDescription { get; set; } = string.Empty;
        public string AudioUrl { get; set; } = string.Empty;
    }

    public class PoiLocalizationEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "POI là bắt buộc")]
        public int PoiId { get; set; }

        [Required(ErrorMessage = "Ngôn ngữ là bắt buộc")]
        public string Language { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên bản dịch là bắt buộc")]
        public string LocalizedName { get; set; } = string.Empty;

        public string LocalizedDescription { get; set; } = string.Empty;
        public string AudioUrl { get; set; } = string.Empty;
    }

    public class AiUsageLimitViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime UsageDate { get; set; }
        public int Count { get; set; }
        public int MaxCount { get; set; } = 100;
        public int ProgressPercent => MaxCount <= 0 ? 0 : Math.Min(100, (int)Math.Round(Count * 100d / MaxCount));
    }

    public class AiUsageLimitEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "UserId là bắt buộc")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Ngày sử dụng là bắt buộc")]
        public DateTime UsageDate { get; set; } = DateTime.Today;

        [Range(0, int.MaxValue, ErrorMessage = "Số lượt phải lớn hơn hoặc bằng 0")]
        public int Count { get; set; }
    }

    public class AuditLogItemViewModel
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ProfileViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Username là bắt buộc")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vai trò là bắt buộc")]
        public string Role { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới")]
        [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận chưa khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
