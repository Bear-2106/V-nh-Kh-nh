using FoodGuideAdmin.Models;
using Microsoft.AspNetCore.Mvc;

namespace FoodGuideAdmin.Controllers
{
    public class HomeController : AdminControllerBase
    {
        public async Task<IActionResult> Index(string category = "All")
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            var poiTask = GetListAsync<PoiItemViewModel>("POIs");
            var foodTask = GetListAsync<FoodItemViewModel>("FoodItems");
            var userTask = GetListAsync<AdminUserViewModel>("AdminUsers");
            var registrationTask = GetListAsync<OwnerRegistrationViewModel>("PoiOwnerRegistrations");
            var submissionTask = GetListAsync<PoiSubmissionViewModel>("PoiSubmissions");
            var auditTask = GetListAsync<AuditLogItemViewModel>("AuditLogs");
            var monitoringTask = GetItemAsync<MonitoringSummaryViewModel>("VisitorMonitoring/summary");

            await Task.WhenAll(poiTask, foodTask, userTask, registrationTask, submissionTask, auditTask, monitoringTask);

            var pois = poiTask.Result;
            var foods = foodTask.Result;
            var categories = pois
                .Select(x => string.IsNullOrWhiteSpace(x.Category) ? "Chưa phân loại" : x.Category.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            var filteredPois = category == "All"
                ? pois
                : pois.Where(x => string.Equals(x.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();

            var totalFiltered = filteredPois.Count;
            var categorySummaries = filteredPois
                .GroupBy(x => string.IsNullOrWhiteSpace(x.Category) ? "Chưa phân loại" : x.Category.Trim())
                .Select(group => new DashboardCategorySummary
                {
                    Category = group.Key,
                    PoiCount = group.Count(),
                    Percentage = totalFiltered == 0 ? 0 : group.Count() * 100d / totalFiltered
                })
                .OrderByDescending(x => x.PoiCount)
                .ToList();

            var poiFoodSummaries = filteredPois
                .GroupJoin(
                    foods,
                    poi => poi.Id,
                    food => food.PoiId,
                    (poi, poiFoods) => new DashboardPoiFoodSummary
                    {
                        PoiId = poi.Id,
                        PoiName = poi.Name,
                        FoodCount = poiFoods.Count()
                    })
                .OrderByDescending(x => x.FoodCount)
                .ThenBy(x => x.PoiName)
                .Take(6)
                .ToList();

            var model = new DashboardViewModel
            {
                TotalPois = pois.Count,
                TotalFoods = foods.Count,
                TotalAdminUsers = userTask.Result.Count,
                TotalOnlineUsers = monitoringTask.Result?.TotalOnlineUsers ?? 0,
                TotalVisitEventsToday = monitoringTask.Result?.TotalVisitEventsToday ?? 0,
                PendingRegistrations = registrationTask.Result.Count(x => string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase)),
                PendingSubmissions = submissionTask.Result.Count(x => string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase)),
                SelectedCategory = string.IsNullOrWhiteSpace(category) ? "All" : category,
                Categories = categories,
                CategorySummaries = categorySummaries,
                PoiFoodSummaries = poiFoodSummaries,
                RecentAuditLogs = auditTask.Result
                    .Where(x => !string.Equals(x.Action, "LOGIN", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(5)
                    .ToList()
            };

            ViewBag.CurrentUser = GetCurrentUser();
            return View(model);
        }
    }
}
