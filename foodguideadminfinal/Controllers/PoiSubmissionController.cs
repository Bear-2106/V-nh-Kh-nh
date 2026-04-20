using FoodGuideAdmin.Models;
using Microsoft.AspNetCore.Mvc;

namespace FoodGuideAdmin.Controllers
{
    public class PoiSubmissionController : AdminControllerBase
    {
        public async Task<IActionResult> Index(int page = 1, int pageSize = 5, string search = "", string status = "Pending")
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            var items = await GetListAsync<PoiSubmissionViewModel>("PoiSubmissions");
            var filtered = items.Where(x =>
                    (string.IsNullOrWhiteSpace(search)
                     || x.PoiName.Contains(search, StringComparison.OrdinalIgnoreCase)
                     || x.Address.Contains(search, StringComparison.OrdinalIgnoreCase)
                     || x.Description.Contains(search, StringComparison.OrdinalIgnoreCase))
                    && (status == "All" || string.Equals(x.Status, status, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(x => x.CreatedAt);

            ViewBag.SelectedStatus = status;
            return View(BuildPagedResult(filtered, page, pageSize, search));
        }

        [HttpGet]
        public async Task<IActionResult> Approve(int id)
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            var result = await SendWithoutBodyAsync(HttpMethod.Put, $"PoiSubmissions/{id}/approve");
            SetAlert(result.Success ? "success" : "danger", result.Success ? "Duyệt bài đăng thành công." : $"Duyệt thất bại: {result.Error}");
            return RedirectToAction(nameof(Index), new { status = "Pending" });
        }

        [HttpGet]
        public async Task<IActionResult> Reject(int id)
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            var result = await SendWithoutBodyAsync(HttpMethod.Put, $"PoiSubmissions/{id}/reject");
            SetAlert(result.Success ? "success" : "danger", result.Success ? "Từ chối bài đăng thành công." : $"Từ chối thất bại: {result.Error}");
            return RedirectToAction(nameof(Index), new { status = "Pending" });
        }
    }
}
