using FoodGuideAdmin.Models;
using Microsoft.AspNetCore.Mvc;

namespace FoodGuideAdmin.Controllers
{
    public class AuditLogController : AdminControllerBase
    {
        public async Task<IActionResult> Index(int page = 1, int pageSize = 5, string search = "", string actionFilter = "All", string resourceFilter = "All")
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            var logs = await GetListAsync<AuditLogItemViewModel>("AuditLogs");
            var filtered = logs.Where(x =>
                (string.IsNullOrWhiteSpace(search)
                    || x.Description.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || x.Action.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || x.Resource.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || x.UserId?.ToString() == search)
                && (actionFilter == "All" || string.Equals(x.Action, actionFilter, StringComparison.OrdinalIgnoreCase))
                && (resourceFilter == "All" || string.Equals(x.Resource, resourceFilter, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(x => x.CreatedAt);

            ViewBag.ActionOptions = logs.Select(x => x.Action).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList();
            ViewBag.ResourceOptions = logs.Select(x => x.Resource).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList();
            ViewBag.SelectedAction = actionFilter;
            ViewBag.SelectedResource = resourceFilter;

            return View(BuildPagedResult(filtered, page, pageSize, search));
        }
    }
}
